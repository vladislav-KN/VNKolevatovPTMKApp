# VNKolevatovPTMKApp
## Тестовое задание

Для запуска в установить [Docker](https://www.docker.com/) и в нём развернуть **PostgreSQL** при помощи команды из основной директории:
```
docker-compose up
```
Для подключения к базе данных **PostgreSQL** использовать appsettings.json пример:
```
{
  "ConnectionStrings": {
    "DataBaseConnection": "Server=localhost;Port=5432;User ID=postgres;Password=123;",
    "DataBaseName": "UserData"
  }
}
```
* **DataBaseConnection** - Подключение к базе данных
* **DataBaseName** - Наименование базы данных
