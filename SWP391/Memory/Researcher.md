# Researcher Module Design

## Scope

The **Researcher** role is a single analytical persona focused on research trend intelligence.

It is meant to:
- analyze publication trends over time
- monitor journals and keywords in depth
- discover emerging topics
- review publication statistics and changes over time

It is **not** meant to become an admin role, a social feature set, or a full search platform.

---

## What Makes Researcher Different

Compared with Lecturer / Student, Researcher should emphasize:
- deeper trend analysis
- historical comparison across years or periods
- topic discovery and topic evolution
- journal and keyword monitoring
- insight-oriented reporting

This role should feel like a research analytics persona, not just another user type with the same features.

---

## In Scope

### 1. Dashboard Insights

Show a focused analytics dashboard for research monitoring.

Core widgets:
- publication trend over time
- citation trend over time
- trending keywords
- trending topics
- top journals by publication volume

Suggested visualizations:
- line chart for time-based trends
- bar chart for top entities
- comparison view for selected keywords or topics

### 2. Topic Discovery

Help researchers find new directions.

Core features:
- trending topics
- emerging topics by growth rate
- related topics based on OpenAlex metadata
- compare 2 keywords or 2 topics by publication count, citation count, and growth rate

### 3. Watchlist / Following

Allow researchers to monitor what they care about.

Core follow targets:
- keyword
- journal
- research topic

Minimal follow behavior:
- save follow target
- list followed items
- use followed items as input for trend and notification checks

### 4. Notifications

Keep this lightweight and research-focused.

Notification types:
- new paper matches a followed keyword
- new paper published in a followed journal
- new paper appears in a followed topic
- trend spike is detected for a followed topic or keyword

### 5. Reports

Provide simple analytical output.

Report types:
- publication trend report
- keyword comparison report
- journal publication summary

Export can remain simple if needed.

---

## Out of Scope

These should not be part of the initial Researcher scope:
- admin user management
- system configuration
- API source management
- AI recommendation
- machine learning prediction
- H-index calculation
- collaboration graph
- ORCID integration
- full-text indexing
- PDF parsing
- paper upload
- real-time synchronization
- social/community features
- author follow in the first pass
- complex export pipelines

---

## Recommended Priority

### Phase 1
- dashboard insights
- publication trend
- citation trend
- trending keywords
- trending topics

### Phase 2
- emerging topics
- related topics
- keyword/topic comparison

### Phase 3
- keyword/journal/topic watchlist
- notifications for new matches

### Phase 4
- simple reports
- export if still needed

---

## Database Impact

For the trimmed Researcher scope, the current database is already enough.

Reuse the existing tables:
- User
- Journal
- Keyword
- ResearchTopic
- Paper
- PublicationTrend
- TrendSnapshot
- Author
- Follow
- Notification
- DashboardReport

No new table is required unless we later add persisted research preferences, saved comparisons, or custom alert rules.

---

## Final Position

Researcher should be implemented as a focused analytics role.

The goal is not to add many features, but to make the existing trend pipeline feel expert-level, research-oriented, and clearly different from Lecturer / Student.