using System.Reflection;
using CorrugatedIron;
using CorrugatedIron.Models;
using Tweets.Attributes;
using Tweets.ModelBuilding;
using Tweets.Models;

namespace Tweets.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string bucketName;
        private readonly IRiakClient riakClient;
        private readonly IMapper<User, UserDocument> userDocumentMapper;
        private readonly IMapper<UserDocument, User> userMapper;

        public UserRepository(IRiakClient riakClient, IMapper<User, UserDocument> userDocumentMapper, IMapper<UserDocument, User> userMapper)
        {
            this.riakClient = riakClient;
            this.userDocumentMapper = userDocumentMapper;
            this.userMapper = userMapper;
            bucketName = typeof (UserDocument).GetCustomAttribute<BucketNameAttribute>().BucketName;
        }

        public void Save(User user)
        {
            var userDocument = userDocumentMapper.Map(user);
            riakClient.Put(new RiakObject("users", userDocument.Id, userDocument));
        }

        public User Get(string userName)
        {
            var user = riakClient.Get("users", userName);
            
            if (!user.IsSuccess)
                return null;

            return userMapper.Map(user.Value.GetObject<UserDocument>());
        }
    }
}