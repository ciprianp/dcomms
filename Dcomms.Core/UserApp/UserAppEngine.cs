﻿using Dcomms.DataModels;
using Dcomms.DMP;
using Dcomms.DRP;
using Dcomms.Vision;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dcomms.UserApp
{
    public class UserAppEngine :IDisposable, IVisibleModule
    {
        const string VisionChannelSourceId = "UA";
        DrpPeerEngine _drpPeerEngine;
        UserAppDatabase _db;
        readonly VisionChannel _visionChannel;
        public string Status => $"todo";

        public UserAppEngine(VisionChannel visionChannel, string databaseBasePath)
        {
            _visionChannel = visionChannel;
            _visionChannel.ClearModules();
         
            _visionChannel.RegisterVisibleModule(VisionChannelSourceId, "UA", this);

            _drpPeerEngine = new DrpPeerEngine(new DrpPeerEngineConfiguration
            {
                VisionChannel = _visionChannel,
                VisionChannelSourceId = VisionChannelSourceId              
            });

            _db = new UserAppDatabase(_drpPeerEngine.CryptoLibrary, new EmptyDatabaseKeyProvider(), _visionChannel, VisionChannelSourceId, databaseBasePath);

           // TestAddLocalUser();

            LocalUsers = new List<LocalUser>();
            foreach (var u in _db.GetLocalUsers())
            {
                var rootUserKeys = _db.GetRootUserKeys(u.Id);
                var userRegistrationIDs = _db.GetUserRegistrationIDs(u.Id);
                if (rootUserKeys != null)
                {
                    LocalUsers.Add(new LocalUser
                    {
                        User = u,
                        RootUserKeys = rootUserKeys,
                        UserRegistrationIDs = userRegistrationIDs,
                    });
                }
            }
        }
        void TestAddLocalUser()
        {
            UserRootPrivateKeys.CreateUserId(3, 2, TimeSpan.FromDays(367), _drpPeerEngine.CryptoLibrary, out var userRootPrivateKeys, out var userId);           
            var userCertificateWithPrivateKey = UserCertificate.GenerateKeyPairsAndSignAtSingleDevice(_drpPeerEngine.CryptoLibrary, userId, userRootPrivateKeys, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddYears(1));

            var u = new User
            {
                AliasID = $"localUser{new Random().Next(1000)}",
                UserID = userId,
                OwnerLocalUserId = 0,
                LocalUserCertificate = userCertificateWithPrivateKey,
            };
            _db.InsertUser(u);

            _db.InsertRootUserKeys(new RootUserKeys
            {
                UserId = u.Id,
                UserRootPrivateKeys = userRootPrivateKeys
            });

            RegistrationId.CreateNew(_drpPeerEngine.CryptoLibrary, out var regPrivateKey, out var registrationId);

            _db.InsertUserRegistrationID(new UserRegistrationID
            {
                UserId = u.Id,
                RegistrationId = registrationId,
                RegistrationPrivateKey = regPrivateKey
            });

        }
        public void Dispose()
        {
            _db?.Dispose();
            _db = null;
        }

        public List<LocalUser> LocalUsers;
        public void DeleteLocalUser(LocalUser localUser)
        {
            foreach (var regId in localUser.UserRegistrationIDs)
                _db.DeleteRegistrationId(regId.Id);
            if (localUser.RootUserKeys != null) _db.DeleteRootUserKeys(localUser.RootUserKeys.Id);
            _db.DeleteUser(localUser.User.Id);

            LocalUsers.Remove(localUser);
        }
    }

    public class LocalUser
    {
        public User User;
        public RootUserKeys RootUserKeys;
        public List<UserRegistrationID> UserRegistrationIDs;
    }

    class EmptyDatabaseKeyProvider : IDatabaseKeyProvider
    {
        public byte[] HsmOperation(byte[] input)
        {
            return input;
        }
    }
}
