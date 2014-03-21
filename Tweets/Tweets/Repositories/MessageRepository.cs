using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class MessageRepository : SqlRepositoryBase, IMessageRepository
    {
        private readonly IMapper<Message, MessageDocument> messageDocumentMapper;
        private readonly IUserRepository userRepository;

        public MessageRepository(IMapper<Message, MessageDocument> messageDocumentMapper,
            IUserRepository userRepository)
        {
            this.messageDocumentMapper = messageDocumentMapper;
            this.userRepository = userRepository;
        }

        public void Save(Message message)
        {
            var messageDocument = messageDocumentMapper.Map(message);
            UseDatabase(context => context.GetTable<MessageDocument>().InsertOnSubmit(messageDocument));
        }

        public void Like(Guid messageId, User user)
        {
            var likeDocument = new LikeDocument {MessageId = messageId, UserName = user.Name, CreateDate = DateTime.UtcNow};
            UseDatabase(context => context.GetTable<LikeDocument>().InsertOnSubmit(likeDocument));
        }

        public void Dislike(Guid messageId, User user)
        {
            UseDatabase(context =>
            {
                var like = context.GetTable<LikeDocument>()
                                  .FirstOrDefault(l => l.MessageId == messageId && l.UserName == user.Name);
                if (like != null)
                    context.GetTable<LikeDocument>().DeleteOnSubmit(like);
            });
        }

        public IEnumerable<Message> GetPopularMessages()
        {
            IEnumerable<Message> result = null;
            UseDatabase(context =>
                result = GetMessagesWithLikes(context).Select(m => new 
                    {
                        message = new Message
                        {
                            Id = m.Message.Id,
                            CreateDate = m.Message.CreateDate,
                            Text = m.Message.Text,
                            Likes = m.Likes.Count()
                        },
                        userName = m.Message.UserName
                    })
                .OrderByDescending(m => m.message.Likes)
                .Take(10)
                .ToArray()
                .Select(m => SetUserInMessage(m.message, m.userName))
                .ToArray()
            );
            return result;
        }

        public IEnumerable<UserMessage> GetMessages(User user)
        {
            IEnumerable<UserMessage> result = null;
            UseDatabase(context =>
                result = GetMessagesWithLikes(context)
                .Where(m => m.Message.UserName == user.Name)
                .Select(m => new
                    {
                        message = new UserMessage
                        {
                            Id = m.Message.Id,
                            CreateDate = m.Message.CreateDate,
                            Text = m.Message.Text,
                            Likes = m.Likes.Count(),
                            Liked = m.Likes.Any(l => l.UserName == user.Name)
                        },
                        userName = m.Message.UserName
                    })
                .OrderByDescending(m => m.message.CreateDate)
                .ToArray()
                .Select(m => SetUserInMessage(m.message, m.userName))
                .ToArray()
            );
            return result;
        }

        private T SetUserInMessage<T>(T message, string userName) where T : Message
        {
            message.User = userRepository.Get(userName);
            return message;
        }

        private IQueryable<MessageWithLikes> GetMessagesWithLikes(DataContext context)
        {
            return from m in context.GetTable<MessageDocument>()
                join l in context.GetTable<LikeDocument>()
                    on m.Id equals l.MessageId into messagesWithLikes
                from ml in messagesWithLikes.DefaultIfEmpty()
                group ml by new {m.Id, m.CreateDate, m.Text, m.UserName}
                into grouped
                select new MessageWithLikes
                {
                    Message = new MessageModel
                    {
                        Id = grouped.Key.Id,
                        CreateDate = grouped.Key.CreateDate,
                        Text = grouped.Key.Text,
                        UserName = grouped.Key.UserName
                    },
                    Likes = grouped.Where(l => l.MessageId != null).AsEnumerable()
                };
        }

        private class MessageWithLikes
        {
            public MessageModel Message { get; set; }
            public IEnumerable<LikeDocument> Likes { get; set; } 
        }

        private class MessageModel
        {
            public Guid Id { get; set; }
            public string UserName { get; set; }
            public string Text { get; set; }
            public DateTime CreateDate { get; set; }
        }
    }
}