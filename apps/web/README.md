# CodeRoom Web

Минимальный Angular-клиент для проверки совместного документа через SignalR.

## Запуск

1. Запустите API с профилем `http` (`http://localhost:5085`).
2. В `apps/web` выполните `npm ci` и `npm start`.
3. Откройте `http://localhost:4200`, введите ID существующих проекта и файла и нажмите «Подключиться».

Angular dev server проксирует `/hubs` на API через `proxy.conf.json`, включая WebSocket. Если API слушает другой адрес, измените `target` в этом файле.

Клиент подписывается на `DocumentUpdateReceived` до запуска соединения, затем вызывает `JoinFile(projectId, fileId)` и `GetCollaborativeDocumentUpdatesAsync(projectId, fileId)`. Для пустого файла результат `[]`, счётчик исходных обновлений равен 0. Обновления из истории и событий декодируются из Base64 и применяются к `Y.Doc`; текст хранится в `Y.Text` с ключом `content`. `text.observe` обновляет поле редактирования, а `document.on('update')` отправляет только локальные изменения через `UpdateCollaborativeDocumentAsync`. Новые события считаются отдельно.
