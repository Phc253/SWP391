- trước khi vào xem xét các problems bên dưới hãy đọc lại toàn bộ dự án để có cái nhìn tổng quát 
- chú ý ở folder Memory
- lưu ý là chưa bắt tay vào code mà xem xét kĩ đề ra hướng giải quyết trước 
- xem xét kĩ lại khi các vấn đề có mối liên quan đến nhau 
- hạn chế migration database mới. nếu bắt buộc vào tình thế đố hay hỏi lại ý kiến của tôi 
- suggest bạn đọc thử AuthImprovementPlan.md
*** vấn đề thứ nhất ***
- API PUT Scheduler config cho role admin truyền vào enble, keyword, maxresult và interval hours kèm 2 API scheduler-config enable và disable thì nghiệp vụ nghe là khá ổn nhưng khi lên UI bên FE tôi thấy vấn đề là:
    - khi admin chọn setting rồi thì phải chạy cái API PUT Scheduler config để lưu config trước sau đó mới có 2 nút là enable và disable cái scheduler. như vậy là sẽ có 3 nút của cái admin phần config đấy, bạn thấy nghiệp vụ như vậy có ổn không hay là nên gộp phần config vào nút enable luôn cho chuyển nghiệp(bên frontend khá thích gộp vào).
*** vấn đề thứ 2 ***
- API datasync/sync-openalex chức năng của API này là gọi đến URL kèm keyword được lấy từ config của admin nhưng nó chỉ lấy bài báo theo top citation mặc định.
- Ví dụ: search với keyword là game với maxresult là 20 thì nó sẽ fetch về 20 bài báo đầu tiên trong mục các bài báo liên quan đến game nhưng nó có 2 vấn đề.
    - bởi vì đây là api synchronize nên fetch đã hơi tốt hơn nghiệp vụ của chữ sync rồi(khá tốt) chính vì thế sau khi qua lần sync thứ 2 (gọi api API datasync/sync-openalex) cùng với maxresult = 20 thì nó vẫn chỉ lấy là 20 bài báo với citation từ cao nhất xuống ( được xếp mặc định trên openalex ) bởi vì bài báo các năm trở lại đây sẽ không thể lọt top citation bằng các bài báo lâu năm được 
- riêng phần này tôi có đề nghị hướng giải quyết thế này:
    - sẽ có thêm 1 api nữa là fetchdata/openalex. API này sẽ chịu trách nhiệm fetch. cũng có thể có Schedule config kèm on/off config đó luôn nhưng phải fetch những thứ hiện tại mà API sync có thể fetch chứ không fetch thêm các trường thông tin khác (fetch các trường data bằng với API datasync). tham khảo thử file: ConfirmFetchnSync.md
    - còn cái API datasync đương nhiên cũng sẽ cùng sửa đổi để có thể tương ứng với những thứ đã fetch về vì hiện tại cái sync đó nó có thể đồng bộ dữ liệu rồi (nhưng chỉ citation thôi vì đó là cái sẽ được tăng lên và dữ liệu bài báo không cần quan tâm vì dự án không cần xử lí kĩ càng bài báo mà là metadata thôi) 