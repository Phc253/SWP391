

🔹 Business Flow 1 — Data Acquisition & Synchronization
(Người 1 – Data Pipeline Owner)
🎯 Business Goal
Thu thập dữ liệu học thuật và xây dựng nguồn dữ liệu nghiên cứu trung tâm.
Main Workflow
External Academic APIs
→ Scheduler Trigger (Configured by Admin)
→ Sync Job Created
→ Fetch Metadata
→ Validate & Normalize
→ Store Database
→ Match Followed Topics/Journals & Trigger Notifications 
Business Value
Tự động cập nhật bài báo mới.
Không phụ thuộc nhập liệu thủ công.
Nền tảng dữ liệu cho toàn hệ thống.
Related Modules
External APIs (OpenAlex, Crossref, Semantic Scholar)
Data Synchronization
Metadata Processing
Core Tables
ApiDataSources
SyncJobs
Papers
Authors
Journals
Keywords
Key Idea
👉 Không có Flow này → toàn bộ hệ thống không hoạt động.

🔹 Business Flow 2 — Research Discovery & User Interaction
(Người 2 – User Experience Owner)
🎯 Business Goal
Cho phép người dùng tìm kiếm và khám phá tri thức khoa học.
Main Workflow
User Login
→ Search Papers
→ Filter (Keyword / Author / Journal)
→ View Details
→ Save/Bookmark (Papers, Keywords)
→ Subscribe/Follow (Journals, Topics)
Business Value
Người dùng tìm bài báo nhanh.
Cá nhân hóa trải nghiệm nghiên cứu.
Tăng mức độ sử dụng hệ thống.
Functional Features
Authentication
Search Engine
Paper Detail View
Bookmark
Follow Topic
Core Tables
Users
Roles
Papers
PaperAuthors
PaperKeywords
Bookmarks
Follows
Key Idea
👉 Đây là Business Flow tương tác trực tiếp với người dùng.

🔹 Business Flow 3 — Trend Analysis & Intelligence Engine
(Người 3 – Analytics / AI Owner)
🎯 Business Goal
Biến dữ liệu bài báo thành tri thức phân tích.
Main Workflow
Collect Metadata
→ Group by Topic / Keyword
→ Count Publications per Year
→ Calculate Growth Rate
→ Generate Trend Score
→ Store Trend Data
Business Value
Phát hiện chủ đề nghiên cứu hot.
Theo dõi sự phát triển khoa học theo thời gian.
Hỗ trợ decision-making học thuật.
Analytical Outputs
Publication Trend
Topic Growth
Trending Keywords
Research Activity Score
Core Tables
PublicationTrends
TrendSnapshots
ResearchTopics
Keywords
Papers
Key Idea
👉 Đây là trái tim phân tích của hệ thống.

🔹 Business Flow 4 — Visualization, Reporting & Administration
(Người 4 – System Management Owner)
🎯 Business Goal
Hiển thị insight + vận hành hệ thống.
Main Workflow
Load Trend Data
→ Render Dashboard
→ Generate Reports
→ Notifications (Consume triggered alerts)
→ System Configuration (Configure API Sources & Sync Schedule)
→ Admin Management
Business Value  
Người dùng hiểu xu hướng ngay lập tức.
Hệ thống có khả năng quản trị.
Cung cấp báo cáo nghiên cứu.
Functional Areas
Dashboard Visualization
Reports Export
Notification System
Admin Panel (including System Configuration)
System Monitoring
Core Tables
DashboardReports
Notifications
ActivityLogs
Users
SyncJobs
Key Idea
👉 Đây là lớp Presentation + System Control.


