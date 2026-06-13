Topic : Scientific Journal Publication Trend Tracking System

Context : Trong bối cảnh số lượng bài báo khoa học và journal học thuật ngày 
càng gia tăng, việc theo dõi xu hướng nghiên cứu, chủ đề nổi bật và sự phát triển của các lĩnh vực học thuật trở nên khó khăn 
đối với giảng viên, sinh viên và nhà nghiên cứu. Các nền tảng học thuật hiện nay chủ yếu hỗ trợ tìm kiếm bài báo nhưng chưa tập trung 
nhiều vào việc phân tích xu hướng công bố theo thời gian và trực quan hóa dữ liệu nghiên cứu.

Problems :"""Khó theo dõi sự thay đổi và phát triển của các chủ đề nghiên cứu theo thời gian do số lượng bài báo khoa học ngày càng lớn.
Việc tìm kiếm bài báo trên các nền tảng học thuật hiện nay chủ yếu dựa trên keyword, chưa hỗ trợ phân tích xu hướng nghiên cứu một cách trực quan.
Giảng viên, sinh viên và nhà nghiên cứu mất nhiều thời gian để xác định các chủ đề đang nổi bật hoặc có tiềm năng nghiên cứu."""
Primary Actors : """- Researcher: Phân tích xu hướng nghiên cứu, theo dõi journal và keyword chuyên sâu, khám phá các chủ đề mới nổi, và xem thống kê công bố theo thời gian.
- Lecturer / Student: Tìm kiếm bài báo tham khảo, khám phá các chủ đề phổ biến, lưu bài báo hoặc keyword quan tâm, và xem dashboard xu hướng cơ bản.
- System Administrator: Quản lý tài khoản người dùng, cấu hình nguồn dữ liệu API, cập nhật dữ liệu bài báo và quản lý hệ thống."""

Functional Req: 
"""User authentication and authorization
Search research papers by keyword, author, or journal
View paper details and publication information
Track publication trends by keyword or topic
Display charts and dashboard statistics
View trending research topics
//Save bookmarks for papers or keywords
//Follow journals or research topics
Receive notifications for newly published papers
Generate simple analytical reports
Synchronize data from external academic APIs (include citation for trending)
Manage users and system configuration (Admin)"""

Main Entities: 
"User
Research Paper
Journal
Keyword
Research Topic
Publication Trend
Author
Bookmark
Notification
Dashboard Report
API Data Source"

Notes : "- Hệ thống sử dụng dữ liệu công khai từ các nguồn học thuật như Semantic Scholar, OpenAlex hoặc Crossref thông qua API miễn phí.
- Login cần thêm các trường thông tin như số điện thoại, địa chỉ, ... (sẽ có xác thực bằng gmail)
- Chỉ thu thập metadata của bài báo, bao gồm: tiêu đề, abstract, keywords, năm xuất bản, tác giả và journal.
- không cần lấy html hay pdf gốc của bài báo
- Không xử lý toàn văn (full-text) của bài báo do giới hạn bản quyền và dung lượng dữ liệu.
- Dữ liệu được giả định là hợp lệ, có cấu trúc thống nhất và luôn khả dụng từ API bên thứ ba.
- Hệ thống chỉ phân tích dữ liệu thuộc một số lĩnh vực được chọn trước (ví dụ: Computer Science hoặc AI) để giảm độ phức tạp.
- Tần suất cập nhật dữ liệu được giả định theo chu kỳ định kỳ (ví dụ: mỗi ngày hoặc mỗi tuần), không yêu cầu realtime."""
- Có khoảng thời gian để đồng bộ dữ liệu với nguồn dữ liệu (VD: citation của bài báo A lúc 10 giờ là 3,000 nhưng sau 2 tiếng đã tăng lên 3,500 => hệ thống sẽ đồng bộ lại dữ liệu)