﻿using System;
using System.Net;
using CommunityBridge3.LiveConnect.Public;

namespace CommunityBridge3.LiveConnect.Internal.Operations
{
  internal class ApiWriteOperation : ApiOperation
    {
        #region Constructors

        public ApiWriteOperation(
            LiveConnectClient client, 
            Uri url, 
            ApiMethod method, 
            string body, 
            SynchronizationContextWrapper syncContext)
            : base(client, url, method, body, syncContext)
        {
        }

        #endregion

        #region Protected methods

        protected override void OnExecute()
        {
            if (this.PrepareRequest())
            {
                try
                {
                    this.Request.BeginGetRequestStream(this.OnGetRequestStreamCompleted, null);
                }
                catch (WebException exception)
                {
                    if (exception.Status == WebExceptionStatus.RequestCanceled)
                    {
                        this.OnCancel();
                    }
                    else
                    {
                        this.OnWebResponseReceived(exception.Response);
                    }
                }
            }
        }

        #endregion
    }
}
