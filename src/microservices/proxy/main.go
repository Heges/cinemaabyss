package main

import (
	"encoding/json"
	"hash/fnv"
	"log"
	"math/rand"
	"net"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"strconv"
	"strings"
	"time"
)

type config struct {
	Port                   string
	MonolithURL            *url.URL
	MoviesServiceURL       *url.URL
	EventsServiceURL       *url.URL
	GradualMigration       bool
	MoviesMigrationPercent int
}

type gateway struct {
	cfg           config
	monolithProxy http.Handler
	moviesProxy   http.Handler
	eventsProxy   http.Handler
}

func main() {
	cfg := loadConfig()
	app := gateway{
		cfg:           cfg,
		monolithProxy: newReverseProxy("monolith", cfg.MonolithURL),
		moviesProxy:   newReverseProxy("movies-service", cfg.MoviesServiceURL),
		eventsProxy:   newReverseProxy("events-service", cfg.EventsServiceURL),
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/health", app.handleHealth)
	mux.HandleFunc("/api/movies", app.handleMovies)
	mux.HandleFunc("/api/movies/", app.handleMovies)
	mux.HandleFunc("/api/events", app.handleEvents)
	mux.HandleFunc("/api/events/", app.handleEvents)
	mux.HandleFunc("/", app.handleMonolith)

	server := &http.Server{
		Addr:              ":" + cfg.Port,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}

	log.Printf(
		"proxy started on port %s; monolith=%s movies=%s events=%s gradual_migration=%t movies_migration_percent=%d",
		cfg.Port,
		cfg.MonolithURL.String(),
		cfg.MoviesServiceURL.String(),
		cfg.EventsServiceURL.String(),
		cfg.GradualMigration,
		cfg.MoviesMigrationPercent,
	)

	log.Fatal(server.ListenAndServe())
}

func loadConfig() config {
	return config{
		Port:                   getEnv("PORT", "8000"),
		MonolithURL:            mustParseURL(getEnv("MONOLITH_URL", "http://localhost:8080")),
		MoviesServiceURL:       mustParseURL(getEnv("MOVIES_SERVICE_URL", "http://localhost:8081")),
		EventsServiceURL:       mustParseURL(getEnv("EVENTS_SERVICE_URL", "http://localhost:8082")),
		GradualMigration:       getEnvBool("GRADUAL_MIGRATION", true),
		MoviesMigrationPercent: getEnvPercent("MOVIES_MIGRATION_PERCENT", 0),
	}
}

func newReverseProxy(name string, target *url.URL) http.Handler {
	proxy := httputil.NewSingleHostReverseProxy(target)
	originalDirector := proxy.Director

	proxy.Director = func(req *http.Request) {
		originalHost := req.Host
		originalDirector(req)
		req.Host = target.Host
		req.Header.Set("X-Forwarded-Host", originalHost)
		req.Header.Set("X-Proxy-Upstream", name)
	}

	proxy.ModifyResponse = func(resp *http.Response) error {
		resp.Header.Set("X-Proxy-Upstream", name)
		return nil
	}

	proxy.ErrorHandler = func(w http.ResponseWriter, r *http.Request, err error) {
		log.Printf("upstream=%s method=%s path=%s error=%v", name, r.Method, r.URL.RequestURI(), err)
		writeJSON(w, http.StatusBadGateway, map[string]string{
			"error":    "bad_gateway",
			"upstream": name,
		})
	}

	return proxy
}

func (g gateway) handleHealth(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "Method not allowed", http.StatusMethodNotAllowed)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"status": true})
}

func (g gateway) handleMovies(w http.ResponseWriter, r *http.Request) {
	if g.routeMoviesToService(r) {
		log.Printf("route method=%s path=%s upstream=movies-service percent=%d", r.Method, r.URL.RequestURI(), g.cfg.MoviesMigrationPercent)
		g.moviesProxy.ServeHTTP(w, r)
		return
	}

	log.Printf("route method=%s path=%s upstream=monolith percent=%d", r.Method, r.URL.RequestURI(), g.cfg.MoviesMigrationPercent)
	g.monolithProxy.ServeHTTP(w, r)
}

func (g gateway) handleEvents(w http.ResponseWriter, r *http.Request) {
	log.Printf("route method=%s path=%s upstream=events-service", r.Method, r.URL.RequestURI())
	g.eventsProxy.ServeHTTP(w, r)
}

func (g gateway) handleMonolith(w http.ResponseWriter, r *http.Request) {
	log.Printf("route method=%s path=%s upstream=monolith", r.Method, r.URL.RequestURI())
	g.monolithProxy.ServeHTTP(w, r)
}

func (g gateway) routeMoviesToService(r *http.Request) bool {
	if !g.cfg.GradualMigration {
		return false
	}

	percent := g.cfg.MoviesMigrationPercent
	if percent <= 0 {
		return false
	}
	if percent >= 100 {
		return true
	}

	if key := stableRoutingKey(r); key != "" {
		return hashPercent(key) < percent
	}

	return rand.Intn(100) < percent
}

func stableRoutingKey(r *http.Request) string {
	for _, header := range []string{"X-User-Id", "X-Session-Id"} {
		if value := strings.TrimSpace(r.Header.Get(header)); value != "" {
			return header + ":" + value
		}
	}

	for _, cookieName := range []string{"session_id", "SessionId"} {
		if cookie, err := r.Cookie(cookieName); err == nil && strings.TrimSpace(cookie.Value) != "" {
			return cookieName + ":" + cookie.Value
		}
	}

	if value := strings.TrimSpace(r.Header.Get("Authorization")); value != "" {
		return "Authorization:" + value
	}

	host, _, err := net.SplitHostPort(r.RemoteAddr)
	if err == nil && host != "" {
		return "ip:" + host
	}

	return strings.TrimSpace(r.RemoteAddr)
}

func hashPercent(value string) int {
	h := fnv.New32a()
	_, _ = h.Write([]byte(value))
	return int(h.Sum32() % 100)
}

func mustParseURL(raw string) *url.URL {
	parsed, err := url.Parse(raw)
	if err != nil {
		log.Fatalf("invalid upstream URL %q: %v", raw, err)
	}
	if parsed.Scheme == "" || parsed.Host == "" {
		log.Fatalf("invalid upstream URL %q: scheme and host are required", raw)
	}
	return parsed
}

func getEnv(key, fallback string) string {
	if value := strings.TrimSpace(os.Getenv(key)); value != "" {
		return value
	}
	return fallback
}

func getEnvBool(key string, fallback bool) bool {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return fallback
	}

	parsed, err := strconv.ParseBool(value)
	if err != nil {
		log.Printf("invalid bool env %s=%q, using %t", key, value, fallback)
		return fallback
	}

	return parsed
}

func getEnvPercent(key string, fallback int) int {
	value := strings.TrimSpace(os.Getenv(key))
	if value == "" {
		return fallback
	}

	parsed, err := strconv.Atoi(value)
	if err != nil {
		log.Printf("invalid percent env %s=%q, using %d", key, value, fallback)
		return fallback
	}

	if parsed < 0 {
		return 0
	}
	if parsed > 100 {
		return 100
	}
	return parsed
}

func writeJSON(w http.ResponseWriter, status int, payload any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	if err := json.NewEncoder(w).Encode(payload); err != nil {
		log.Printf("failed to write response: %v", err)
	}
}
