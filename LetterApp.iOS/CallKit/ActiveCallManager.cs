﻿using System;
using System.Collections.Generic;
using CallKit;
using Foundation;

namespace LetterApp.iOS.CallKit
{
    public class ActiveCallManager
    {
        #region Private Variables
        private CXCallController CallController = new CXCallController();
        #endregion

        #region Computed Properties
        public List<ActiveCall> Calls { get; set; }
        #endregion

        #region Constructors
        public ActiveCallManager()
        {
            // Initialize
            this.Calls = new List<ActiveCall>();
        }
        #endregion

        #region Private Methods
        private void SendTransactionRequest(CXTransaction transaction)
        {
            // Send request to call controller
            CallController.RequestTransaction(transaction, (error) => {
                // Was there an error?
                if (error == null)
                {
                    // No, report success
                    Console.WriteLine("Transaction request sent successfully.");
                }
                else
                {
                    // Yes, report error
                    Console.WriteLine("Error requesting transaction: {0}", error);
                }
            });
        }
        #endregion

        #region Public Methods
        public ActiveCall FindCall(NSUuid uuid)
        {
            // Scan for requested call
            foreach (ActiveCall call in Calls)
            {
                if (call.UUID == uuid) return call;
            }

            // Not found
            return null;
        }

        public void StartCall(string contact)
        {
            // Build call action
            var handle = new CXHandle(CXHandleType.PhoneNumber, contact);
            var startCallAction = new CXStartCallAction(new NSUuid(), handle);

            // Create transaction
            var transaction = new CXTransaction(startCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void EndCall(ActiveCall call)
        {
            // Build action
            var endCallAction = new CXEndCallAction(call.UUID);

            // Create transaction
            var transaction = new CXTransaction(endCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void PlaceCallOnHold(ActiveCall call)
        {
            // Build action
            var holdCallAction = new CXSetHeldCallAction(call.UUID, true);

            // Create transaction
            var transaction = new CXTransaction(holdCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }

        public void RemoveCallFromOnHold(ActiveCall call)
        {
            // Build action
            var holdCallAction = new CXSetHeldCallAction(call.UUID, false);

            // Create transaction
            var transaction = new CXTransaction(holdCallAction);

            // Inform system of call request
            SendTransactionRequest(transaction);
        }
        #endregion
    }
}