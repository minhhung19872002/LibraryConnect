-- Chạy một lần khi container postgres khởi tạo dữ liệu lần đầu.
-- Các extension bắt buộc cho tra cứu tiếng Việt không dấu (mục 4.11 của đặc tả).
CREATE EXTENSION IF NOT EXISTS unaccent;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Các schema nghiệp vụ. EF Core cũng tự tạo khi chạy migration, khai báo sẵn ở đây
-- để script sao lưu/phục hồi thủ công vẫn dựng được cấu trúc rỗng.
CREATE SCHEMA IF NOT EXISTS sys;
CREATE SCHEMA IF NOT EXISTS cat;
CREATE SCHEMA IF NOT EXISTS bib;
CREATE SCHEMA IF NOT EXISTS acq;
CREATE SCHEMA IF NOT EXISTS ser;
CREATE SCHEMA IF NOT EXISTS dig;
CREATE SCHEMA IF NOT EXISTS rdr;
CREATE SCHEMA IF NOT EXISTS cir;
CREATE SCHEMA IF NOT EXISTS web;
CREATE SCHEMA IF NOT EXISTS ill;
