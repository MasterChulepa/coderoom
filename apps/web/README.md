# CodeRoom Web

Минимальный Angular-клиент для проверки совместного документа через SignalR.

## Запуск

1. Запустите API с профилем `http` (`http://localhost:5085`).
2. В `apps/web` выполните `npm ci` и `npm start`.
3. Откройте `http://localhost:4200`, введите ID существующих проекта и файла и нажмите «Подключиться».

Angular dev server проксирует `/hubs` на API через `proxy.conf.json`, включая WebSocket. Если API слушает другой адрес, измените `target` в этом файле.

Клиент подписывается на `DocumentUpdateReceived` до запуска соединения, затем вызывает `JoinFile(projectId, fileId)` и `GetCollaborativeDocumentUpdatesAsync(projectId, fileId)`. Для пустого файла результат `[]`, счётчик исходных обновлений равен 0. Новые события считаются отдельно. Пока содержимое обновлений не применяется к Yjs.
