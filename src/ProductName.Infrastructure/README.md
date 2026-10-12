## Getting Started
The Infrastructure project provides concrete implementations for external capabilities required by the application. 
It connects application-level interfaces to technologies such as databases, files, APIs, logging providers, operating-system features, or hardware SDKs.

Infrastructure-specific details should remain in this project so they do not leak into the Domain or WPF projects.

Typical contents: database contexts and repositories, file storage, API clients, logging adapters, and integrations with external libraries.
## Responsibility
SQLite, file, logging adapter, API, adapter devices, and other external services.

## Dependencies
AppCore and Domain

## NOTE
Ghi chú về Infrastructure: Đây là ranh giới phù hợp để đặt các tích hợp bên ngoài, nhưng chưa cần viết adapter hoặc thêm package database/device khi chưa có tính năng tương ứng. 
Với ứng dụng rất nhỏ chưa có tích hợp, bạn có thể tạo project này khi bắt đầu tích hợp đầu tiên thay vì để một project trống tồn tại lâu dài.