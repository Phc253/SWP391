- lỗi hiện tại checkpoint của API manual fetch data đang bị lỗi trả về 500 ném ra (Error: No such host is known. (api.openalex.org:443)) [1]
- suy đoán: có thể sẽ kéo theo lỗi của API enable scheduler config.
- nếu cần phải migration thêm bảng thì phải hỏi ý kiến của tôi.
*** vấn đề của API sync data ***
- tuy sync rất mượt mà nhưng 100 bài báo thì tốn thời gian load khá lâu khoảng 20 - 30s 
==> không biết có thể thêm checkpoint tương tự như API fetch để nó chạy lần lượt nhằm tăng tốc độ không (cần tối ưu hóa tốc độ)
- API có trả về 3 trường: 
    "externalRecordsFetched": 0,
    "recordsInserted": 0,
    "recordsUpdated": 0,
    "recordsFetched": 0,
- tuy nhiên vẫn chưa biết chính xác bài báo nào được cập nhật hay cập nhật cái gì, dữ liệu được cập nhật là gì ,
được tăng từ bao nhiêu lên bao nhiêu, trong khoảng thời gian cập nhật cách nhau bao nhiêu, bài báo thuộc topic nào ... đại loại vậy (cần chi tiết hơn nữa để hiển thị lên thành bảng sau khi admin sync thành công)
*** vấn đề của API fetch data (checkpoint) ***
- hiện tại khi set useCheckpoint = false thì fetch về khá mượt nhưng khi giá trị được set là true thì nó trả về lỗi như trên [1] bởi vì nó có liên quan đến schedule config enalbe nên tôi cần bạn kiểm tra lại "kĩ càng khúc này"
- ngoài ra trong vài trường hợp cụ thể như  maxResults = 2 và useCheckpoint = true thì nó trả Error như sau: (The JSON value could not be converted to System.Int32. Path: $.meta.page | LineNumber: 0 | BytePositionInLine: 65.")
- bên cạnh đó không biết Fetch có cần hiển thị thêm thông tin gì khác nữa cho specific hay không ( điều này cần ý kiến của bạn sau khi đọc lại khái quát toàn bộ dự án kĩ càng )
