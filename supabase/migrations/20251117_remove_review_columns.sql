-- Xóa các cột không cần thiết khỏi bảng reviews
ALTER TABLE reviews DROP COLUMN IsApproved;
ALTER TABLE reviews DROP COLUMN Type;
ALTER TABLE reviews DROP COLUMN ProductId;