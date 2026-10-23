# Portable Bulk File Renamer

Ứng dụng WPF C# dành cho Windows 10/11 x64 để đổi tên hàng loạt file cục bộ. Giao diện có Tiếng Việt/English. Ứng dụng không cần kết nối mạng và không gửi tên file hoặc nội dung file ra ngoài.

## Chức năng

- Chọn thư mục (mặc định không quét thư mục con) hoặc chọn nhiều file từ nhiều thư mục.
- Lọc danh sách, chọn/bỏ chọn tất cả, chỉ giữ các file đã chọn.
- Tạo nhiều quy tắc đổi tên và sắp xếp thứ tự chạy.
- Chế độ tìm kiếm: Anywhere, Prefix, Suffix, Exact Position.
- Thao tác: Replace, Remove, Add Prefix, Add Suffix, Uppercase, Lowercase.
- Vị trí bắt đầu từ 1; có thể đếm từ đầu hoặc cuối tên cơ sở; phần mở rộng được loại khỏi phép tìm và giữ nguyên.
- Xem trước, kiểm tra tên không hợp lệ, trùng tên đích, đích đã tồn tại và trường hợp quy tắc không khớp.
- Xác nhận trước khi đổi tên, lưu lịch sử và hỗ trợ hoàn tác lô gần nhất.

## Build bằng Visual Studio

1. Mở `PortableBulkFileRenamer.csproj` bằng Visual Studio 2022/2026 có workload **.NET desktop development**.
2. Chọn cấu hình `Release` và nền tảng `x64` (hoặc Any CPU; publish vẫn chỉ định `win-x64`).
3. Build hoặc dùng script `publish-win-x64.bat` trên Windows có .NET 8 SDK.

## Publish một EXE portable

Trên Windows với .NET 8 SDK, chạy:

```powershell
dotnet publish PortableBulkFileRenamer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish/win-x64
```

EXE nằm trong `publish/win-x64`. Nên kiểm tra trên máy Windows 10 x64 trước khi phân phối. Single-file self-contained không có nghĩa là đã được kiểm thử trên mọi máy.

## GitHub Actions

Workflow ở `.github/workflows/build.yml` chạy khi push lên `main` hoặc khi chạy thủ công từ tab **Actions**. Artifact `BulkFileRenamer-win-x64` chứa EXE publish.

## Safety notes

- Chạy thử với bản sao dữ liệu trước khi đổi tên thư mục/file quan trọng.
- Đổi tên chỉ áp dụng cho các dòng được chọn và có trạng thái `Ready` sau Preview.
- Nếu một quy tắc tìm kiếm không khớp, file đó không được đánh dấu Ready.
- Hoàn tác cần đường dẫn file đích còn tồn tại và đường dẫn gốc còn trống.
- Đây là source code được cung cấp để build; chưa khẳng định đã biên dịch hoặc kiểm thử thành công trong môi trường này.
