# Flow 1 Status - 2026-05-29

Current implementation:
- Manual OpenAlex metadata fetch is exposed through `POST /api/datasync/fetch-openalex`.
- `POST /api/datasync/sync-openalex` remains as a backward-compatible alias for the metadata fetch.
- OpenAlex citation-count refresh is separated into `POST /api/datasync/synchronize-citations`.
- The endpoint now uses `DataSyncService` instead of calling `AcademicDataIntegrationService` directly.
- The endpoint is protected by the `AdminOnly` authorization policy.
- Each manual sync creates a `SyncJobs` row with `Running` status.
- Successful fetches update the job to `Completed`.
- Fetches where ingestion succeeds but notification matching fails update the job to `CompletedWithWarnings`.
- Failed syncs update the job to `Failed` and store the error message.
- Optional scheduled OpenAlex maintenance is available through `DataSyncSchedulerHostedService` and the `DataSyncScheduler` appsettings section.
- The scheduler now runs both metadata fetch and citation synchronization.
- Metadata ingestion now returns newly saved paper IDs so downstream services can act only on new papers.
- After ingestion, `DataSyncService` triggers `NotificationTriggerService` to notify users following matching journals or research topics.
- Notifications are stored in the existing `Notifications` table with `RelatedType = "Paper"` and `RelatedId = PaperId`; app-level duplicate checks prevent repeat user-paper notifications.
- `DataSyncResponse` includes `NotificationsCreated` for admin visibility.

Still pending:
- Add database-level duplicate protection for paper external IDs, preferably a unique index on `(SourceId, ExternalId)` after checking existing duplicates.
- Consider database-level duplicate protection for notifications, such as a filtered unique index on `(UserId, RelatedType, RelatedId)` for `RelatedType = 'Paper'`.
- Consider persisting richer sync metrics if the reporting/admin UI needs them later.
