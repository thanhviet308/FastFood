-- Check if there are any reviews in the database
SELECT COUNT(*) as TotalReviews FROM reviews;

-- Check reviews by type and approval status
SELECT 
    Type,
    IsApproved,
    COUNT(*) as Count
FROM reviews 
GROUP BY Type, IsApproved;

-- Show some sample reviews
SELECT TOP 5 
    Id,
    Rating,
    Content,
    UserName,
    UserEmail,
    CreatedAt,
    IsApproved,
    Type
FROM reviews 
ORDER BY CreatedAt DESC;