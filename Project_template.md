## Изучите [README.md](.\README.md) файл и структуру проекта.

# Задание 1

1. Контейнераная диаграмма Cinema Abyss(Кинобездна)
[CinemaAbyss_Container](docs\to-be\diagrams\container\CinemaAbyss_Container.puml)

# Задание 2

### 1. Proxy
Реализован сервис на GO выступающий как API Gateway для балансировки на основе MOVIES_MIGRATION_PERCENT.

### 2. Kafka
 Вам как архитектуру нужно также проверить гипотезу насколько просто реализовать применение Kafka в данной архитектуре.

Для этого нужно сделать MVP сервис events, который будет при вызове API создавать и сам же читать сообщения в топике Kafka.

    - Разработан сервис на ASP.NET Core Web API, с распределенной очередью продюсера и консьюмера.
    - Реализуован простой API для создания Events для Kafka, User/Payment/Movie и обрабатывается внутри сервиса с записью в лог
    - Добавлен docker и интегрирован в общий docker-compose

Необходимые тесты для проверки этого API из папки tests/postman проходят, все зеленое.
1. Скриншот состояния postman tests
[postman-tests](docs\postman-tests.png)

2. Скриншот состояния kafka topics state
[topics-state](docs\topics-state.png)