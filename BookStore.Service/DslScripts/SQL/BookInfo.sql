SELECT
    b.ID,
    NumberOfComments = COUNT(c.ID)
FROM
    Bookstore.Book b
    LEFT JOIN Bookstore.Comment c ON c.BookID = b.ID
GROUP BY
    b.ID