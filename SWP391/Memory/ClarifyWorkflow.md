hôm nay sẽ là buổi hoàn thiện workflow nhằm trình bày trước lớp vào thứ 5 sắp tới. có vài vấn đề tôi vẫn đang cần làm rõ sau đây 
# ta sẽ bắt đầu với workflow 2 trước 
- tôi muốn gộp register vào trong workflow 2 luôn 
=> flow tôi nghĩ nên là: register -> verify -> login -> search papers -> filter ( keyword, author, journal ) -> view detail -> save/bookmark ( papers, keywords ) -> subcribe/follow ( journals, topics ) -> view follow/bookmark 
- ngay tại chỗ từ filter keyword, author, journal tôi cần hỏi ý kiến bạn khi gom 3 cái đó vào 1 filter thì nó có chuyên nghiệp hay không, có nên tách ra mỗi cái 1 API không, nó có gây cản trở cho frontend không. 
- và ngay sau đó là chỗ view detail của bài báo tôi mong muốn là bài báo khi filter ra sẽ có vài thông tin cơ bản và API view detail sẽ show thêm thông tin chi tiết hơn 1 chút về bài báo đấy ( có phải là chức năng của API /api/Papers/{id} không ) 
# tiếp đến là workflow 1 
- ở đây workflow mong muốn tôi vẫn chưa xác định được. mặc dù workflow ban đầu đề ra đã code đã coverage được gần hết 
- ngay chỗ schedule trigger tôi vẫn chưa nắm bắt rõ nguyên lí hoạt động. nhưng ý tưởng của tôi là khi role admin được đăng nhập thì sẽ có 1 button gọi đến 1 API có trách nhiệm on/off cái chỗ synchronize data. ( chỗ này bạn đọc thật kĩ lại cái Memory/ProjecOverview.md xem là cái fetch meta data của bài báo thì có cần auto trigger không hay chỉ có cái đồng bộ dữ liệu cần auto trigger ) 
- tiếp đến là cái API tự động đồng bộ dữ liệu với open alex. theo như tôi tracking tiến độ thì có vẻ là chưa có API đấy. 
- workflow 1 ngay cái chỗ Schedule trigger nếu muốn là tự động tôi nghĩ sẽ để 1 tuần 1 lần sau đó cái syncjob sẽ có ý nghĩa hơn là lưu lại lịch sử. 
- phần Fetch Metadata, Validate & Normalize và Store Database theo như tôi đang thấy là 1 API Data sync đang xử lí hết quy trình đấy, thì khi tôi lên trình bày công đoạn đấy sẽ khá khó nói vì chỉ có 1 API nên tôi phải mở code ra nói từng công đoạn.
- có 1 chỗ → Match Followed Topics/Journals & Trigger Notifications này thì tôi cũng chưa thấy API xử lí nó luôn và cũng hơi mơ hồ requirement chỗ này ( cái trigger thì tôi hiểu 1 chút và thấy nó khá là chuẩn nghiệp vụ nên bạn cần xem xét lại cái match followed theo tôi nghĩ là chỉ đang xác nhận lại là người đó có đang follow hay bookmark nào không thi gửi về notification về ) 
### ngoài ra còn 1 số lưu ý khác khá quan trọng tôi thấy bạn cần sửa đổi 
- endpoint của các API tôi thấy nên để viết thường 
- ngoài ra khi test bằng swagger hay postman tôi thấy đa số các API get của tôi đang gửi dữ liệu qua param điều này làm dữ liệu dễ bị lộ không biết có cách nào khác để gửi dữ liệu qua body không ? ( tôi đang xem xét đổi thành method post vì post luôn gửi dữ liệu qua body ) 
- khi thuyết trình review API tôi muốn các bước trong workflow tôi đảm nhiệm (1 & 2) được API hóa càng nhiều càng tốt bởi vì thầy chưa xem xét kĩ nghiệp vụ chỉ kiểm tra xem với workflow đề ra từ ban đầu thì đã bao phủ đủ các API để xử lí chưa thôi 