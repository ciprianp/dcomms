﻿using Dcomms.UserApp.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dcomms.UserApp
{
    public class Contact
    {
        string _userAliasID;
        public string UserAliasID // used by web UI only
        {
            get
            {
                return _userAliasID ?? User?.AliasID;
            }
            set
            {
                _userAliasID = value;
            }
        }
        public User User; 
    }
}