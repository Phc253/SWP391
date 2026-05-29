# Flow 1 Status - 2026-05-29

Current implementation:
- Manual OpenAlex sync is exposed through `POST /api/datasync/sync-openalex`.
- The endpoint now uses `DataSyncService` instead of calling `AcademicDataIntegrationService` directly.
- The endpoint is protected by the `AdminOnly` authorization policy.
- Each manual sync creates a `SyncJobs` row with `Running` status.
- Successful syncs update the job to `Completed`.
- Syncs where ingestion succeeds but trend computation fails update the job to `CompletedWithWarnings`.
- Failed syncs update the job to `Failed` and store the error message.
- After metadata ingestion, the pipeline calls `TrendService.ComputeTrendsAsync()` to refresh `PublicationTrends`.

Still pending:
- Add an optional scheduler trigger for periodic sync.
- Add database-level duplicate protection for paper external IDs, preferably a unique index on `(SourceId, ExternalId)` after checking existing duplicates.
- Generate notifications for users following journals or research topics when newly ingested papers match their follows.
- Consider persisting richer sync metrics if the reporting/admin UI needs them later.
