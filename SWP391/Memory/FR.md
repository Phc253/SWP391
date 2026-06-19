1. Hiểu đúng bản chất hệ thống (System Core)

Topic:
Scientific Journal Publication Trend Tracking System

👉 Đây không phải paper search engine.

👉 Đây là:

Academic Analytics Platform
+
Research Trend Intelligence System

CORE VALUE của system:

Tracking publication trends
Research topic analytics
Visualization & insights

Paper search chỉ là support feature.

2. Xác định Actor → Capability Matrix

👨‍🔬 Researcher
Phân tích xu hướng sâu
Theo dõi topic/journal
Xem analytics nâng cao

👩‍🎓 Lecturer / Student
Tìm paper
Bookmark
Xem dashboard cơ bản

⚙️ Administrator
Quản lý hệ thống
Đồng bộ API
Quản trị user

3. Functional Requirement Decomposition (Quan trọng nhất)

MODULE 1 — User & Access Management

FR-01 User Registration
Create account
Email/password authentication

FR-02 User Login / Logout
Authenticate user session

FR-03 Authorization
Role-based access:
Researcher
Lecturer/Student
Admin

FR-04 User Profile Management
Update profile info
Change password

FR-05 Admin User Management
View users
Activate/deactivate users
Assign roles

MODULE 2 — Research Paper Discovery

FR-06 Search Papers
Search by:
keyword
author
journal
topic

FR-07 Filter Results
year
research field
journal
relevance

FR-08 View Paper Details
Display:
title
abstract
authors
keywords
journal
publication year
citation info (metadata only)

MODULE 3 — Research Topic & Trend Tracking ⭐ (CORE MODULE)

FR-09 Track Publication Trend
View publication count over time
Trend by keyword
Trend by research topic

FR-10 Trending Topics Detection
Identify hot topics
Rank topics by growth rate

FR-11 Historical Trend Analysis
Compare trends across years
Show topic evolution

MODULE 4 — Visualization & Dashboard

FR-12 Dashboard Overview
Display:
total papers
active journals
trending keywords
publication growth

FR-13 Chart Visualization
Charts:
line chart (trend over time)
bar chart (topic popularity)
pie chart (journal distribution)

FR-14 Personalized Dashboard
User-specific analytics view

MODULE 5 — Bookmark & Following System

FR-15 Bookmark Paper
User saves paper

FR-16 Bookmark Keyword
Save research interest

FR-17 Follow Journal
Track journal updates

FR-18 Follow Research Topic

MODULE 6 — Notification System

FR-19 Subscribe to Updates
Based on:
followed journal
followed topic
keyword

FR-20 Receive Notification
When:
new related paper published
trend spike detected

FR-21 Notification Management
view notifications
mark as read

MODULE 7 — Analytical Reporting

FR-22 Generate Analytical Report
Generate simple reports:
keyword trend report
journal publication report

FR-23 Export Report
PDF / CSV (simple export)

MODULE 8 — External Academic Data Synchronization

FR-24 Connect External APIs
Supported sources:
Semantic Scholar
OpenAlex
Crossref

FR-25 Scheduled Data Sync
daily / weekly sync

FR-26 Metadata Import
Import only:
title
abstract
authors
journal
keywords
year

FR-27 Sync Monitoring
Admin can:
view sync status
retry sync
view logs

MODULE 9 — System Administration

FR-28 Configure Data Sources
Enable/disable APIs

FR-29 Manage Research Fields
Limit system scope (AI, CS…)

FR-30 System Configuration
update frequency
dashboard settings

4. Functional Requirement Hierarchy (Final View)
Scientific Trend Tracking System
│
├── User Management
├── Paper Discovery
├── Trend Analytics ⭐
├── Visualization Dashboard
├── Bookmark & Follow
├── Notification
├── Reporting
├── API Synchronization
└── Administration