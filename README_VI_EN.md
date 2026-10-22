# Portable Bulk File Renamer

Ứng dụng WPF đổi tên file hàng loạt cho Windows 10/11 64-bit. Xử lý cục bộ, không có chức năng kết nối mạng hoặc tải dữ liệu lên.

## Tính năng hiện có
- Chọn một thư mục (có thể quét thư mục con) hoặc chọn nhiều file.
- Thêm tiền tố, hậu tố.
- Tìm và thay thế cụm từ; tùy chọn phân biệt chữ hoa/thường.
- Xóa cụm từ.
- Đánh số tự động với số bắt đầu và số chữ số tùy chỉnh.
- Giữ nguyên phần mở rộng theo mặc định.
- Xem trước tên mới và cảnh báo tên không hợp lệ/trùng tên/đích đã tồn tại.
- Xác nhận trước khi đổi tên.
- Ghi lịch sử JSON và hoàn tác batch gần nhất trong phiên ứng dụng.
- Giao diện Việt/Anh.

## Build file EXE Portable
Yêu cầu trên máy build: Windows và .NET 8 SDK. Máy chạy EXE không cần cài .NET khi phát hành self-contained.

1. Giải nén thư mục dự án.
2. Cài .NET 8 SDK trên máy build nếu chưa có.
3. Nhấp đúp `publish-win-x64.bat`.
4. File phát hành nằm tại `publish\\win-x64\\BulkFileRenamer.exe`.
5. Chép EXE sang USB hoặc thư mục khác để chạy. Không cần installer.

Có thể chạy lệnh tương đương trong thư mục dự án:

```powershell
dotnet publish PortableBulkFileRenamer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\\win-x64
```

## An toàn và giới hạn
- Chỉ đổi tên file; không đọc nội dung file.
- Không gửi dữ liệu ra ngoài.
- Trước khi chạy trên dữ liệu quan trọng, thử trên một thư mục sao chép.
- Tool không tự tạo bản sao lưu nội dung file. Undo dùng lịch sử đường dẫn và chỉ hoạt động khi file đã đổi tên vẫn còn ở đường dẫn đích, đường dẫn gốc chưa bị chiếm.
- Lịch sử được ghi vào thư mục `History` cạnh EXE nếu có quyền ghi; nếu không, dùng `%LOCALAPPDATA%\\BulkFileRenamer`.
- Nếu chọn thư mục con, mọi file trong đó có thể được đưa vào danh sách; kiểm tra preview kỹ trước khi xác nhận.
- Windows có thể yêu cầu xác nhận khi chạy EXE tải từ Internet, tùy chính sách bảo mật.

## Current validation status
Source project and publish script are provided. The EXE has **not** been built or tested in this environment because the .NET SDK and Windows/WPF build environment are unavailable here. Build on a Windows machine with .NET 8 SDK, then test with copied sample files before production use.
