# Набор служб для синхронизации данных клиентов и их карт между базой Анализатора и CRM

## Состав

### AnalizToCrmClientInfoConsumer

Загрузка данных клиентов из Kafka в миграционную базу на .Net Core

### CrmToAnalizClientInfoProducer

Выгрузка обновлённых в CRM данных клиентов в Kafka на .Net Core

### CrmToAnalizClientInfoConsumer

Загрузка обновлённых в CRM данных клиентов в Kafka в базу Анализатора

### AnalizToCrmCardsConsumer

Загрузка карт из Kafka в миграционную базу на .Net Core

### CrmToAnalizCardsProducer *

Выгрузка обновлённых в CRM данных карт клиентов в Kafka на .Net Core

### CrmToAnalizCardsConsumer *

Загрузка обновлённых в CRM данных карт клиентов в Kafka в базу Анализатора на .Net Core

### SyncLibrary 

Общий код для работы с брокером

### AnalyzToCrmSyncRunner

Сервис синхронизации, развёрнутый в подсети Анализатора

### SyncRunnerCrmToAnaliz

Сервис синхронизации, развёрнутый в подсети миграционной базы CRM