# Confirm Fetch and Sync Direction

## 1. Mục đích của tài liệu

Tài liệu này ghi lại rõ tình trạng hiện tại của flow `Data Fetch & Synchronization`, điểm yếu còn tồn tại, và định hướng hoàn thiện sau này.

Hiện tại chưa nên sửa code gấp trước buổi trình bày. Mục tiêu trước mắt là hiểu đúng bản chất hệ thống để trình bày tự tin. Sau này nếu cần nâng cấp, tài liệu này có thể dùng làm context cho developer hoặc AI model khác tiếp tục triển khai.

## 2. Flow hiện tại đang hoạt động như thế nào

Hiện tại API `POST /api/DataSync/sync-openalex` đang được dùng như một pipeline tổng hợp:

```text
Admin / Scheduler trigger
  -> DataSyncService.SyncOpenAlexAsync(keyword, maxResults)
  -> AcademicDataIntegrationService.FetchAndSaveDataFromOpenAlexAsync(keyword, maxResults)
  -> gọi OpenAlex API bằng search keyword
  -> normalize dữ liệu trả về
  -> check duplicate bằng ExternalId
  -> nếu paper mới thì insert Paper / Journal / Author / ResearchTopic / Keyword
  -> nếu paper đã có thì update CitationCount
  -> tạo notification cho user follow journal/topic
  -> recompute trends
  -> update SyncJob status
```

Vì vậy chữ `sync` trong bản hiện tại nên được hiểu là:

```text
Đồng bộ database nội bộ với nguồn ngoài OpenAlex.
```

Nó không chỉ là "sync dữ liệu đã có", mà bao gồm cả fetch dữ liệu mới từ OpenAlex.

## 3. Ý nghĩa thật của maxResults hiện tại

`maxResults` hiện tại là batch size cho mỗi lần gọi OpenAlex.

Ví dụ:

```json
{
  "keyword": "Animals",
  "maxResults": 20,
  "intervalHours": 2
}
```

Nghĩa là:

```text
Cứ mỗi lần sync, hệ thống gọi OpenAlex để lấy tối đa 20 works theo keyword "Animals".
```

Nó không phải là tổng số bài tối đa mà hệ thống được lưu vĩnh viễn.

Tuy nhiên, vì code hiện tại gọi:

```text
/works?search={keyword}&per-page={maxResults}
```

nên kết quả phụ thuộc vào cách OpenAlex xếp hạng search result. Khi test keyword `Animals`, OpenAlex trả về những bài rất nhiều citation trước, ví dụ bài có hơn 64,000 citations. Điều này cho thấy search result không đảm bảo là bài mới nhất.

## 4. Điểm yếu hiện hữu trong sync hiện tại

### 4.1. Chưa đảm bảo fetch được bài mới nhất

Nếu một bài mới xuất hiện nhưng citation thấp hoặc relevance chưa đủ cao, nó có thể không nằm trong top result của:

```text
/works?search=Animals&per-page=100
```

Khi đó dù scheduler chạy định kỳ, hệ thống vẫn có thể không lấy được bài mới đó.

### 4.2. Chưa có incremental sync thật sự

Hiện tại hệ thống chưa lưu:

```text
LastSyncTime theo keyword/source
Cursor phân trang của OpenAlex
LastFetchedPublicationDate
LastUpdatedDate từ nguồn ngoài
```

Do đó mỗi lần sync vẫn gọi lại batch đầu theo search keyword. Nếu batch đầu gần như không đổi, hệ thống sẽ chủ yếu gặp duplicate rồi update citation, thay vì mở rộng dữ liệu mới.

### 4.3. Fetch mới và refresh dữ liệu cũ đang bị gộp vào một flow

Hiện tại một pipeline đang làm cả hai việc:

```text
Fetch new works từ OpenAlex
Refresh citation count cho paper đã tồn tại
```

Về mặt demo thì ổn, nhưng về production thì nên tách rõ:

```text
Fetch New Works = lấy thêm bài mới từ OpenAlex
Refresh Existing Works = cập nhật citation/metadata cho bài đã có trong DB
```

### 4.4. maxResults đang bị clamp tới 200 trong code nhưng OpenAlex per_page max thực tế nên là 100

Trong code hiện tại có clamp `maxResults` tới 200, nhưng OpenAlex API thường giới hạn `per-page` tối đa 100 cho list endpoint. Nếu truyền 200, có thể API tự xử lý, trả lỗi, hoặc không đúng kỳ vọng tùy behavior của OpenAlex.

Về sau nên chỉnh:

```text
maxResults per request <= 100
Nếu cần nhiều hơn thì dùng page/cursor.
```

### 4.5. Topic admin nhập vào thực chất là search keyword

Khi admin nhập `Computer Science`, `Animals`, `Medicine`, giá trị đó hiện đang là search term gửi lên OpenAlex.

Nó chưa phải là filter topic chính xác theo OpenAlex topic id/domain id/field id.

Nói đúng hơn:

```text
Admin keyword = từ khóa tìm kiếm external API
ResearchTopic trong DB = concept/topic được normalize từ metadata OpenAlex trả về
```

Hai khái niệm này liên quan nhưng không giống nhau hoàn toàn.

## 5. Định hướng hoàn thiện đúng hơn sau này

Sau này nên tách hệ thống thành hai job chính.

## 5.1. Job 1: Fetch New Works

Mục tiêu:

```text
Lấy các paper mới từ OpenAlex theo keyword/topic mà admin cấu hình.
```

Luồng đề xuất:

```text
Scheduler / Admin trigger
  -> đọc config keyword/topic
  -> đọc LastSyncTime hoặc LastPublicationDate
  -> gọi OpenAlex với filter ngày
  -> sort theo publication_date desc
  -> dùng cursor/page nếu cần nhiều hơn 100 records
  -> normalize
  -> insert paper mới
  -> update sync checkpoint
```

URL tốt hơn flow hiện tại:

```text
/works?search=Animals
  &filter=from_publication_date:2026-06-01
  &sort=publication_date:desc
  &per-page=100
```

Ý nghĩa:

```text
Chỉ lấy các bài từ ngày 2026-06-01 trở đi,
ưu tiên bài mới nhất,
giảm khả năng bị kẹt ở các bài cũ nhiều citation.
```

Nếu muốn lấy nhiều hơn 100 records:

```text
Sử dụng cursor paging:
/works?search=Animals&filter=...&sort=publication_date:desc&per-page=100&cursor=*
```

Sau mỗi response, lấy `next_cursor` để gọi trang tiếp theo.

## 5.2. Job 2: Refresh Existing Works

Mục tiêu:

```text
Cập nhật lại citation count hoặc metadata của những paper đã có trong database.
```

Luồng đề xuất:

```text
Scheduler / Admin trigger
  -> lấy danh sách paper đã có trong DB
  -> chọn paper cần refresh, ví dụ paper lâu chưa update
  -> gọi OpenAlex bằng ExternalId hoặc DOI
  -> update CitationCount / metadata
  -> lưu LastRefreshedAt
```

Job này không nhằm lấy bài mới. Nó chỉ đảm bảo dữ liệu cũ không bị stale.

Ví dụ:

```text
Paper A đã có trong DB với CitationCount = 100
Sau 1 tuần gọi lại OpenAlex
Nếu OpenAlex trả CitationCount = 125
Thì update DB thành 125
```

## 6. Config nên tách rõ hơn về sau

Hiện tại config đang có:

```text
Enabled
Keyword
MaxResults
IntervalHours
```

Sau này nên cân nhắc tách thành:

```text
FetchNewWorksEnabled
FetchKeyword
FetchBatchSize
FetchIntervalHours
FetchFromDate / LastFetchCheckpoint

RefreshExistingEnabled
RefreshBatchSize
RefreshIntervalHours
RefreshOlderThanDays
```

Ví dụ:

```json
{
  "fetchNewWorksEnabled": true,
  "fetchKeyword": "Animals",
  "fetchBatchSize": 100,
  "fetchIntervalHours": 24,
  "refreshExistingEnabled": true,
  "refreshBatchSize": 50,
  "refreshIntervalHours": 168,
  "refreshOlderThanDays": 7
}
```

Ý nghĩa:

```text
Mỗi ngày fetch bài mới về Animals.
Mỗi tuần refresh citation cho các paper đã có mà hơn 7 ngày chưa update.
```

## 7. Data model nên bổ sung về sau

Để incremental sync tốt hơn, nên bổ sung một bảng hoặc mở rộng `SystemSettings`.

Phương án tốt hơn là tạo bảng riêng, ví dụ:

```text
SyncCheckpoint
```

Các field đề xuất:

```text
CheckpointId
SourceName
Keyword
LastFetchTime
LastPublicationDate
LastCursor
LastRefreshTime
CreatedAt
UpdatedAt
```

Ý nghĩa:

```text
Mỗi keyword/source có checkpoint riêng.
Khi scheduler chạy, nó biết lần trước đã fetch đến đâu.
```

Ngoài ra, bảng `Paper` nên cân nhắc có:

```text
LastSyncedAt
LastCitationRefreshAt
```

để biết paper nào cần refresh lại citation.

## 8. Lưu ý khi xử lý

### 8.1. Không nên chỉ tăng maxResults

Tăng `maxResults` không giải quyết triệt để vấn đề bài mới không lọt top search result.

Ví dụ:

```text
maxResults = 20 -> lấy top 20
maxResults = 100 -> lấy top 100
```

Nhưng nếu bài mới nằm ngoài top 100 vì ít citation/relevance, nó vẫn bị miss.

Giải pháp đúng là:

```text
sort/filter theo publication_date hoặc updated date,
không chỉ dựa vào search ranking.
```

### 8.2. Không nên fetch toàn bộ OpenAlex

OpenAlex có lượng dữ liệu rất lớn. Không nên cố page toàn bộ `/works` chỉ để đồng bộ local database.

Nên giới hạn theo:

```text
keyword/topic
publication date range
batch size
cursor
rate limit
```

### 8.3. Cần phân biệt publication_date và updated_date

`publication_date` phù hợp để tìm bài mới công bố.

`updated_date` hoặc metadata update date, nếu dùng được từ OpenAlex, phù hợp để tìm record đã được OpenAlex cập nhật sau lần sync trước.

Nếu mục tiêu là:

```text
Tìm bài mới -> ưu tiên publication_date
Cập nhật citation/metadata -> ưu tiên updated_date hoặc refresh existing by id
```

### 8.4. Cần tránh tạo notification cho paper cũ khi refresh

Notification chỉ nên tạo cho paper mới insert hoặc paper lần đầu xuất hiện trong hệ thống.

Nếu chỉ refresh citation count của paper cũ, không nên bắn notification "new paper".

### 8.5. Trend computation có thể tách lịch riêng

Hiện tại mỗi lần sync xong đều recompute trends. Với dữ liệu lớn, việc này có thể nặng.

Sau này có thể:

```text
Fetch/insert paper trước
Queue trend recomputation sau
Hoặc recompute trend theo lịch riêng
```

Nhưng với scope hiện tại, recompute ngay sau sync là chấp nhận được.

## 9. Cách trả lời khi bị hỏi trong buổi trình bày

Nếu bị hỏi:

```text
Nếu bài mới không lọt top maxResults thì sao?
```

Trả lời:

```text
Hiện tại flow là batch sync theo keyword, dùng để demo pipeline fetch-normalize-upsert-notification-trend.
Em nhận thức được rằng search result của OpenAlex không đảm bảo luôn là bài mới nhất.
Vì vậy hướng production là bổ sung incremental sync: lưu checkpoint theo keyword/source,
fetch bằng filter ngày như from_publication_date, sort publication_date desc,
và dùng cursor paging nếu cần lấy nhiều hơn một page.
```

Nếu bị hỏi:

```text
Sync khác fetch như thế nào?
```

Trả lời:

```text
Trong bản hiện tại, sync được hiểu là đồng bộ database với OpenAlex nên nó bao gồm fetch mới và upsert.
Nếu mở rộng đúng hơn, em sẽ tách thành hai job:
Fetch New Works để lấy bài mới,
và Refresh Existing Works để cập nhật citation/metadata cho bài đã có.
```

Nếu bị hỏi:

```text
Tăng maxResults lên có giải quyết không?
```

Trả lời:

```text
Không triệt để. maxResults chỉ tăng kích thước batch.
Nếu ranking vẫn ưu tiên bài cũ nhiều citation, bài mới vẫn có thể bị miss.
Giải pháp đúng là filter/sort theo thời gian và lưu checkpoint.
```

## 10. Kết luận

Flow hiện tại đủ tốt để trình bày vì nó đã có đầy đủ pipeline:

```text
Trigger
Fetch OpenAlex
Normalize
Upsert DB
Notification
Trend recomputation
SyncJob audit
```

Nhưng điểm cần nói rõ là:

```text
Đây là batch sync theo keyword, chưa phải incremental sync hoàn chỉnh.
```

Hướng nâng cấp sau này:

```text
Tách Fetch New Works và Refresh Existing Works.
Thêm checkpoint theo keyword/source.
Fetch bài mới bằng publication_date desc hoặc updated date.
Dùng cursor paging cho dữ liệu nhiều.
Giới hạn per-page đúng theo OpenAlex.
Không dùng maxResults như giải pháp duy nhất.
```
