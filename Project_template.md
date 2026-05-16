## Изучите [README.md](.\README.md) файл и структуру проекта.

# Задание 1

1. Контейнераная диаграмма Cinema Abyss(Кинобездна)
[CinemaAbyss_Container](docs\to-be\diagrams\container\CinemaAbyss_Container.puml)

# Задание 2

### 1. Proxy
Реализован сервис на GO выступающий как API Gateway для балансировки на основе MOVIES_MIGRATION_PERCENT.

### 2. Kafka
 Kafka.

- Разработан сервис на ASP.NET Core Web API, с распределенной очередью продюсера и консьюмера.
- Реализуован простой API для создания Events для Kafka, User/Payment/Movie и обрабатывается внутри сервиса с записью в лог
- Добавлен docker и интегрирован в общий docker-compose

Необходимые тесты для проверки этого API из папки tests/postman проходят, все зеленое.
1. Скриншот состояния postman tests
[postman-tests](docs\postman-tests.png)

2. Скриншот состояния kafka topics state
[topics-state](docs\topics-state.png)

# Задание 3
Решение перенесено в Kubernetes.

### CI/CD

 В папке .github/worflows доработайте деплой новых сервисов proxy и events в docker-build-push.yml , чтобы api-tests при сборке отрабатывали корректно при отправке коммита в ваш репозиторий.

Сборка зеленая.

### Proxy в Kubernetes

Добавьте сюда скриншота вывода при вызове https://cinemaabyss.example.com/api/movies и  скриншот вывода event-service после вызова тестов.
1. Скриншот вызова "https://cinemaabyss.example.com/api/movies"
[cinemaabyss_browser](docs\cinemaabyss_browser.png)

2. Скриншот состояний events-service и тестов
[kubernetes-tests](docs\kubernetes-tests.png)

# Задание 4
Решение перенесено в Kubernetes и упаковано в helm.
