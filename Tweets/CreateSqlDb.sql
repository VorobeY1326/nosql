USE NoSql

CREATE TABLE messages
(
	id UniqueIdentifier PRIMARY KEY,
	userName varchar(100),
	[text] varchar(1000),
	createDate datetime,
	version RowVersion
)

CREATE NONCLUSTERED INDEX messagesUserNameIndex ON messages (userName)

CREATE TABLE likes
(
	messageId UniqueIdentifier,
	userName varchar(100),
	createDate datetime,
)

CREATE UNIQUE CLUSTERED INDEX likesPk ON likes (messageId, userName)

ALTER TABLE likes
  ADD CONSTRAINT FK_messageId
  FOREIGN KEY (messageId) REFERENCES [messages](id)