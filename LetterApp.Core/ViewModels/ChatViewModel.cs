﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LetterApp.Core.Exceptions;
using LetterApp.Core.Helpers;
using LetterApp.Core.Helpers.Commands;
using LetterApp.Core.Localization;
using LetterApp.Core.Models;
using LetterApp.Core.Services.Interfaces;
using LetterApp.Core.ViewModels.Abstractions;
using LetterApp.Models.DTO.ReceivedModels;
using SendBird;
using Xamarin.Essentials;

namespace LetterApp.Core.ViewModels
{
    public class ChatViewModel : XViewModel<int>
    {
        private readonly IDivisionService _divisionService;
        private readonly IContactsService _contactService;
        private readonly IMessengerService _messengerService;
        private readonly IDialogService _dialogService;
        private readonly IAudioService _audioService;

        private PreviousMessageListQuery _prevMessageListQuery;
        private GetUsersInDivisionModel _user;
        private ChatListUserModel _userChat;
        private GroupChannel _channel;

        private int _userId;
        private int _organizationId = AppSettings.OrganizationId;
        private string _finalUserId = AppSettings.UserAndOrganizationIds;
        public string MemberName;
        public string MemberDetails;
        public bool NewMessageAlert;

        private ChatModel _chat;
        public ChatModel Chat
        {
            get => _chat;
            set => SetProperty(ref _chat, value);
        }

        private UserModel _thisUser;
        private List<MessagesModel> _messagesModel;
        private List<ChatMessagesModel> _chatMessages;

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private string _status;
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public List<MessagesModel> SendedMessages = new List<MessagesModel>();

        private XPCommand<Tuple<string, MessageType>> _sendMessageCommand;
        public XPCommand<Tuple<string, MessageType>> SendMessageCommand => _sendMessageCommand ?? (_sendMessageCommand = new XPCommand<Tuple<string, MessageType>>(async (msg) => await SendMessage(msg), CanExecuteMsg));

        private XPCommand _viewWillCloseCommand;
        public XPCommand ViewWillCloseCommand => _viewWillCloseCommand ?? (_viewWillCloseCommand = new XPCommand(ViewWillClose));

        private XPCommand _closeViewCommand;
        public XPCommand CloseViewCommand => _closeViewCommand ?? (_closeViewCommand = new XPCommand(async () => await CloseView()));

        private XPCommand<bool> _typingCommand;
        public XPCommand<bool> TypingCommand => _typingCommand ?? (_typingCommand = new XPCommand<bool>(TypingStatus));

        private XPCommand<bool> _loadMessagesUpdateReceiptCommand;
        public XPCommand<bool> LoadMessagesUpdateReceiptCommand => _loadMessagesUpdateReceiptCommand ?? (_loadMessagesUpdateReceiptCommand = new XPCommand<bool>(async (val) => await LoadMessagesAndUpdateReadReceipt(val), CanExecuteLoad));

        private XPCommand _removeChatHandlersCommand;
        public XPCommand RemoveChatHandlersCommand => _removeChatHandlersCommand ?? (_removeChatHandlersCommand = new XPCommand(() => { SendBirdClient.RemoveChannelHandler("ChatHandler"); }));

        private XPCommand _callCommand;
        public XPCommand CallCommand => _callCommand ?? (_callCommand = new XPCommand(async () => await Call()));

        private XPCommand _optionsCommand;
        public XPCommand OptionsCommand => _optionsCommand ?? (_optionsCommand = new XPCommand(async () => await Options()));

        public ChatViewModel(IContactsService contactsService, IMessengerService messengerService, IDialogService dialogService, IDivisionService divisionService, IAudioService audioService)
        {
            _divisionService = divisionService;
            _contactService = contactsService;
            _messengerService = messengerService;
            _dialogService = dialogService;
            _audioService = audioService;

            ChatHandlers();
            CheckConnection();
            SetL10NResources();
        }

        protected override void Prepare(int userId)
        {
            _userId = userId;
        }

        public override async Task Appearing()
        {
            NavigationService.ChatOpen(_userId);

            _isLoading = true;

            string fromDivision = string.Empty;

            _userChat = Realm.Find<ChatListUserModel>(_userId);
            var users = Realm.All<GetUsersInDivisionModel>();
            _user = users?.First(x => x.UserId == _userId);
            _thisUser = Realm.Find<UserModel>(AppSettings.UserId);

            if (_user == null || _thisUser == null)
            {
                _dialogService.ShowAlert(UserNotFound, AlertType.Error, 4f);
                CloseView();
                return;
            }

            MemberName = $"{_user?.FirstName} {_user?.LastName}";
            MemberDetails = _user?.Position;

            if (_thisUser.Divisions.Count > 1)
            {
                var division = Realm.Find<DivisionModelProfile>(_user.MainDivisionId);

                if (division == null)
                {
                    try
                    {
                        division = await _divisionService.GetDivisionProfile(_user.MainDivisionId);
                        Realm.Write(() => Realm.Add(division, true));

                        fromDivision = division?.Name;
                    }
                    catch (Exception ex)
                    {
                        Ui.Handle(ex as dynamic);
                    }
                }
                else
                {
                    fromDivision = division?.Name;
                }
            }

            MessagesLogic(_userChat?.MessagesList);

            var chat = new ChatModel
            {
                MemberId = _userId,
                MemberName = $"{_user?.FirstName} {_user?.LastName}",
                MemberDetails = string.IsNullOrEmpty(fromDivision) ? _user?.Position : $"{fromDivision} - {_user?.Position}",
                MemberPhoto = _user.Picture,
                MemberEmail = _user.Email,
                MemberMuted = _userChat != null && _userChat.IsMemberMuted,
                MemberSeenMyLastMessage = _userChat != null && _userChat.MemberSeenMyLastMessage,
                MemberPresence = Connectivity.NetworkAccess == NetworkAccess.Internet && _userChat != null ? (MemberPresence)_userChat.MemberPresence : MemberPresence.Offline,
                Messages = _chatMessages ?? new List<ChatMessagesModel>(),
                MessageEvent = MessageClickEvent
            };

            _chat = chat;

            LoadMessagesAndUpdateReadReceipt(true);
        }

        private async Task LoadRecentMessages()
        {
            IsLoading = true;

            Debug.WriteLine("LoadRecentMessages");

            if (SendBirdClient.GetConnectionState() != SendBirdClient.ConnectionState.OPEN) 
                await MessageServiceConnection();

            _messagesModel = new List<MessagesModel>();

            _prevMessageListQuery = _channel?.CreatePreviousMessageListQuery();

            try
            {
                var result = await _messengerService.LoadMessages(_prevMessageListQuery);

                if (result != null || result.Count > 0)
                {
                    foreach (BaseMessage message in result)
                    {
                        if (message is UserMessage msg)
                        {
                            var newMessage = new MessagesModel
                            {
                                MessageId = msg.MessageId,
                                MessageType = 0,
                                MessageData = msg.Message,
                                MessageSenderId = msg.Sender.UserId,
                                MessageDateTicks = DateTime.Parse(msg.Data).ToLocalTime().Ticks
                            };

                            _messagesModel.Add(newMessage);
                        }
                        else
                        {
                            if (!(message is FileMessage mensg))
                                continue;

                            var newMessage = new MessagesModel
                            {
                                MessageId = mensg.MessageId,
                                MessageType = mensg.Type == "IMAGE" ? 1 : 2,
                                MessageData = mensg.Url,
                                MessageSenderId = mensg.Sender.UserId,
                                MessageDateTicks = DateTime.Parse(mensg.Data).ToLocalTime().Ticks,
                                CustomData = mensg.Name
                            };

                            _messagesModel.Add(newMessage);
                        }
                    }
                }
                else
                {
                    _isLoading = false; 
                    RaisePropertyChanged(nameof(IsLoading));
                    return;
                }
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
            }
            finally
            {
                _isLoading = false;
                RaisePropertyChanged(nameof(IsLoading));
            }

            MessagesLogic(_messagesModel);

            bool shouldUpdate = false;

            if (_chat.Messages?.Count > 0 && _chatMessages?.Count > 0)
                shouldUpdate = _chat.Messages.Last().MessageId != _chatMessages.Last().MessageId;
            else
                shouldUpdate = true;

            _chat.Messages = _chatMessages;

            if(shouldUpdate)
                RaisePropertyChanged(nameof(Chat));
        }

        private async Task<bool> MessageServiceConnection()
        {
            Debug.WriteLine("Checking Message Service Connection: " + SendBirdClient.GetConnectionState());

            IsBusy = true;

            var tcs = new TaskCompletionSource<bool>();

            try
            {
                if (SendBirdClient.GetConnectionState() != SendBirdClient.ConnectionState.OPEN)
                {
                    int numberOfTries = 0;

                    while (SendBirdClient.GetConnectionState() == SendBirdClient.ConnectionState.CONNECTING && numberOfTries < 4)
                    {
                        Debug.WriteLine("Trying to reconnect to Messenger Services");
                        await Task.Delay(TimeSpan.FromSeconds(numberOfTries++));
                    }

                    if(SendBirdClient.GetConnectionState() != SendBirdClient.ConnectionState.OPEN)
                        await _messengerService.ConnectMessenger();

                }

                await GetChannel();

                return SendBirdClient.GetConnectionState() == SendBirdClient.ConnectionState.OPEN;
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void MessagesLogic(IList<MessagesModel> messagesList, bool shouldKeepOldMessages = false)
        {
            if (messagesList == null || messagesList?.Count == 0)
                return;
                
            if (!shouldKeepOldMessages || _chatMessages == null) {
                _chatMessages = new List<ChatMessagesModel>();
            }

            var messageOrdered = messagesList.OrderBy(x => x.MessageDateTicks).ToList();

            foreach (MessagesModel message in messageOrdered)
            {
                if (string.IsNullOrEmpty(message.MessageData))
                    continue;

                var newMessage = new ChatMessagesModel();
                var massageDate = new DateTime(message.MessageDateTicks);

                if (_chatMessages.Count == 0)
                {
                    newMessage.ShowHeaderDate = true;
                    newMessage.PresentMessage = (PresentMessageType)message.MessageType;
                }
                else
                {
                    newMessage.ShowHeaderDate = _chatMessages[_chatMessages.Count - 1].MessageDateTime.Date != massageDate.Date;

                    if (newMessage.ShowHeaderDate || _chatMessages[_chatMessages.Count - 1].MessageSenderId != message.MessageSenderId)
                        newMessage.PresentMessage = (PresentMessageType)message.MessageType;
                    else
                        newMessage.PresentMessage = (PresentMessageType)(message.MessageType + 3);
                }

                if (newMessage.ShowHeaderDate)
                    newMessage.HeaderDate = DateUtils.DateForChatHeader(massageDate).ToUpper();

                string dateForMessage = L10N.Locale() == "en-US" ? massageDate.ToString("hh:mm tt") : massageDate.ToString("HH:mm");

                newMessage.Name = message.MessageSenderId == _finalUserId ? $"{_thisUser.FirstName} {_thisUser.LastName}" : $"{_user.FirstName} {_user.LastName}";
                newMessage.Picture = message.MessageSenderId == _finalUserId ? _thisUser.Picture : _user.Picture;
                newMessage.MessageId = message.MessageId;
                newMessage.MessageData = message.MessageData;
                newMessage.MessageType = (MessageType)message.MessageType;
                newMessage.MessageSenderId = message.MessageSenderId;
                newMessage.CustomData = message.CustomData;
                newMessage.MessageDate = $"  •  {dateForMessage}";
                newMessage.MessageDateTime = massageDate;
                newMessage.ShowPresense = message.MessageSenderId != _finalUserId; 

                _chatMessages.Add(newMessage);
            }
        }

        private async Task SendMessage(Tuple<string, MessageType> message, bool sendingFailedMessages = false)
        {
            await MessageServiceConnection();

            string messageToSend = message.Item1;

            while(messageToSend.EndsWith(Environment.NewLine)) {
                messageToSend = messageToSend.TrimEnd(Environment.NewLine.ToCharArray());
            }

            try
            {
                var _sendedMessageDateTime = DateTime.UtcNow;

                var fakeMessage = new MessagesModel
                {
                    MessageId = _sendedMessageDateTime.Ticks,
                    MessageData = messageToSend,
                    MessageType = (int)message.Item2,
                    MessageSenderId = _finalUserId,
                    MessageDateTicks = _sendedMessageDateTime.ToLocalTime().Ticks,
                };

                MessagesLogic(new List<MessagesModel> { fakeMessage }, true);

                _chat.Messages = _chatMessages;
                SendedMessages.Add(fakeMessage);

                if (!sendingFailedMessages)
                {
                    RaisePropertyChanged(nameof(SendedMessages));
                    _audioService.PlaySendMessage();
                }

                _status = string.Empty;
                RaisePropertyChanged(nameof(Status));

                var result = await _messengerService.SendMessage(_channel, messageToSend, _sendedMessageDateTime.ToString());

                if(result != null)
                {
                    var sendedmsg = SendedMessages.FirstOrDefault(x => new DateTime(x.MessageDateTicks).Date == DateTime.Parse(result.Data).ToLocalTime().Date);
                    SendedMessages.Remove(sendedmsg);

                    var msg = new MessagesModel
                    {
                        MessageId = result.MessageId,
                        MessageData = result.Message,
                        MessageType = (int)message.Item2,
                        MessageSenderId = result.Sender.UserId,
                        MessageDateTicks = DateTime.Parse(result.Data).ToLocalTime().Ticks
                    };

                    if (_messagesModel == null)
                        _messagesModel = new List<MessagesModel>();

                    _messagesModel.Add(msg);
                }
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);

                foreach (var messageId in SendedMessages) {
                    _chat.Messages.Last(x => x.MessageId == messageId.MessageId).FailedToSend = true;
                }

                _dialogService.ShowAlert(SendMessageError, AlertType.Error, 4f);

                if (!sendingFailedMessages)
                    RaisePropertyChanged(nameof(Chat));
            }
        }

        private void MessageClickEvent(object sender, long messageId)
        {
            var message = SendedMessages.Find(x => x.MessageId == messageId);

            if (message != null)
                RetrySendMessages();
        }

        private async Task RetrySendMessages()
        {
            try
            {
                if (await MessageServiceConnection())
                {
                    var listMsg = new List<MessagesModel>();
                    foreach (var msg in SendedMessages) { listMsg.Add(msg); }

                    foreach(var msg in listMsg)
                    {
                        SendedMessages.Remove(msg);
                        await SendMessage(new Tuple<string, MessageType>(msg.MessageData, (MessageType)msg.MessageType), true);
                    }

                    foreach(var msg in listMsg)
                    {
                        _chat.Messages.Remove(_chat.Messages.Find(x => x.MessageId == msg.MessageId));
                    }

                    listMsg = null;

                    RaisePropertyChanged(nameof(Chat));
                }
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
            }
        }

        public void ChatHandlers()
        {
            Debug.WriteLine("Initializing SendBird Handlers in Chat");

            SendBirdClient.ChannelHandler ch = new SendBirdClient.ChannelHandler
            {
                OnMessageReceived = MessageReceived,
                OnTypingStatusUpdated = TypingStatus,
                OnReadReceiptUpdated = ReadReceipt
            };

            SendBirdClient.AddChannelHandler("ChatHandler", ch);

            Debug.WriteLine("SendBird Handlers Initialized");
        }

        private async Task LoadMessagesAndUpdateReadReceipt(bool loadMessages)
        {
            Debug.WriteLine("LoadMessagesAndUpdateReadReceipt WithLoadMessages:" + SendBirdClient.GetConnectionState());

            if (SendBirdClient.GetConnectionState() != SendBirdClient.ConnectionState.OPEN)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                await MessageServiceConnection();
            }
                
            await GetChannel();

            _messengerService.MarkMessageAsRead(_channel);

            if (loadMessages)
                await LoadRecentMessages();
        }

        private async Task GetChannel()
        {
            try
            {
                if (_channel == null)
                {
                    _channel = await _messengerService.GetCurrentChannel($"{_userId}-{_organizationId}");

                    if (_channel == null)
                        _channel = await _messengerService.CreateChannel(new List<string> { $"{_userId}-{_organizationId}" });
                }

                if (_channel.MemberCount < 2)
                {
                    _messengerService.RemoveChannel(_channel);
                    _dialogService.ShowAlert(UserNotRegistered, AlertType.Error, 4f);

                    if (_userChat != null)
                        Realm.Write(() => Realm.Remove(_userChat));

                    _chat = null;
                    await CloseView();
                }
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
            }
        }

        #region Status

        private void TypingStatus(GroupChannel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (channel.Url == _channel.Url)
                {
                    _status = channel.IsTyping() ? TypingMessage : string.Empty;
                    StatusLogic();
                }
            });
        }

        private void TypingStatus(bool isTyping)
        {
            if (isTyping)
                _messengerService.TypingMessage(_channel);
            else
                _messengerService.TypingMessageEnded(_channel);
        }

        private void MessageReceived(BaseChannel baseChannel, BaseMessage baseMessage)
        {
            MainThread.BeginInvokeOnMainThread(() => {

                Debug.WriteLine("Message Received");

                _chat.MemberPresence = MemberPresence.Online;

                var channel = baseChannel as GroupChannel;

                if (channel?.Url != _channel.Url)
                    return;

                var newMessage = new MessagesModel();

                if (baseMessage is UserMessage msg)
                {
                    newMessage.MessageId = msg.MessageId;
                    newMessage.MessageType = 0;
                    newMessage.MessageData = msg.Message;
                    newMessage.MessageSenderId = msg.Sender.UserId;
                    newMessage.MessageDateTicks = DateTime.Parse(msg.Data).ToLocalTime().Ticks;
                }

                MessagesLogic(new List<MessagesModel> { newMessage }, true);

                _chat.Messages = _chatMessages;
                RaisePropertyChanged(nameof(Chat));

                _status = string.Empty;

                RaisePropertyChanged("Status");
                RaisePropertyChanged("NewMessageAlert");

                _audioService.PlayReceivedMessage();
            });
        }

        private void ReadReceipt(GroupChannel channel)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_chat.MemberPresence != MemberPresence.Online)
                {
                    _chat.MemberPresence = MemberPresence.Online;
                    RaisePropertyChanged("Chat");
                }

                StatusLogic();
            });
        }

        private void StatusLogic()
        {
            if (_status != TypingMessage)
            {
                if (_chat.Messages.Last().MessageSenderId == _finalUserId)
                    _status = _channel.GetReadReceipt(_channel.LastMessage) <= 0 ? SeenMessage : string.Empty;
                else
                    _status = string.Empty;
            }

            RaisePropertyChanged(nameof(Status));
        }

        #endregion

        private async Task CheckConnection()
        {
            Connectivity.ConnectivityChanged -= ConnectivityChanged;
            Connectivity.ConnectivityChanged += ConnectivityChanged;
        }

        private void ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                ConnectionOn();
            }
            else
            {
                _chat.MemberPresence = MemberPresence.Offline;
                RaisePropertyChanged(nameof(Chat));
                SendBirdClient.RemoveChannelHandler("ChatHandler");
            }
        }

        private async Task ConnectionOn()
        {
            await LoadMessagesAndUpdateReadReceipt(true);
            ChatHandlers();
        }

        private async Task Call()
        {
            try
            {
                var callType = await _dialogService.ShowContactOptions(LocationResources, _user.ShowNumber);

                switch (callType)
                {
                    case CallingType.Letter:
                        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
                            await NavigationService.NavigateAsync<CallViewModel, Tuple<int, bool>>(new Tuple<int, bool>(_user.UserId, true));
                        break;
                    case CallingType.Cellphone:
                        CallUtils.Call(_user.ContactNumber);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
            }
        }

        private async Task Options()
        {
            try
            {
                string[] resources = { SeeUserProfile, SendEmail, UserMuted, ArchiveChat, Close };
                var options = await _dialogService.ShowChatOptions(MemberName, _chat.MemberPhoto, _chat.MemberMuted, resources);

                _chat.MemberMuted = options.Item2;

                switch (options.Item1)
                {
                    case ChatOptions.SeeProfile:
                        await NavigationService.NavigateAsync<MemberViewModel, int>(_userId);
                        break;
                    case ChatOptions.SendEmail:
                        await EmailUtils.SendEmail(_chat.MemberEmail);
                        break;
                    case ChatOptions.ArchiveChat:
                        _chat.MemberArchived = true;
                        CloseView();
                        break;
                }

            }
            catch (Exception ex)
            {
                Ui.Handle(ex as dynamic);
            }
        }

        private async Task CloseView() => await NavigationService.Close(this);

        private void ViewWillClose()
        {
            NavigationService.ChatOpen(-1);
            SendBirdClient.RemoveChannelHandler("ChatHandler");

            if (_chat?.Messages?.Count > 0)
            {
                Realm.Write(() =>
                {
                    var lastMessage = _chat.Messages.Last();

                    var userChat = new ChatListUserModel
                    {
                        MemberId = _user.UserId,
                        MemberName = $"{_user.FirstName} {_user.LastName} - {_user.Position}",
                        MemberPhoto = _user.Picture,
                        IsMemberMuted = _chat.MemberMuted,
                        IsNewMessage = false,
                        MemberPresence = (int)_chat.MemberPresence,
                        MemberPresenceConnectionDate = _userChat != null ? _userChat.MemberPresenceConnectionDate : default(DateTime).Ticks,
                        //TODO LastMessage: Add logic for file and images
                        LastMessage = _chat.Messages.Last().MessageSenderId == _finalUserId ? $"{YouChatLabel} {lastMessage.MessageData}" : lastMessage.MessageData,
                        LastMessageDateTimeTicks = lastMessage.MessageDateTime.Ticks,
                        IsArchived = _chat.MemberArchived,
                        ArchivedTime = _chat.MemberMuted ? DateTime.Now.Ticks : default(DateTime).Ticks,
                        UnreadMessagesCount = 0,
                        MemberSeenMyLastMessage = _chat.MemberSeenMyLastMessage,
                    };

                    var historyMessages = _messagesModel?.Skip(Math.Max(0, _messagesModel.Count() - 30)) ?? _userChat?.MessagesList?.Skip(Math.Max(0, _userChat.MessagesList.Count() - 30));

                    foreach (var msg in historyMessages)
                        userChat.MessagesList.Add(msg);

                    Realm.Add(userChat, true);
                });
            }
        }

        private bool CanExecute() => !IsBusy;
        private bool CanExecuteMsg(Tuple<string, MessageType> msg) => !IsBusy;
        private bool CanExecuteLoad(bool arg) => !IsBusy;

        #region Resources 

        private string YouChatLabel => L10N.Localize("ChatList_You");
        private string UserNotRegistered => L10N.Localize("ChatList_UserNotRegistered");
        private string SendMessageError => L10N.Localize("ChatList_MessageError");
        private string UserNotFound => L10N.Localize("ChatList_UserNotFound");
        public string TypeSomething => L10N.Localize("OnBoardingViewModel_LetterSlogan");

        public string SendingMessage => L10N.Localize("Chat_SendingMessage");
        public string TypingMessage => L10N.Localize("Chat_TypingMessage");
        public string SeenMessage => L10N.Localize("Chat_SeenMessage");
        public string SendMessageButton => L10N.Localize("Chat_SendMessage");

        private string SeeUserProfile => L10N.Localize("Chat_UserProfile");
        private string UserMuted => L10N.Localize("Chat_UserMuted");
        private string ArchiveChat => L10N.Localize("Chat_Archive");
        private string SendEmail => L10N.Localize("Division_SendEmail");
        private string Close => L10N.Localize("Chat_Close");

        private Dictionary<string, string> LocationResources = new Dictionary<string, string>();
        private string TitleDialog => L10N.Localize("ContactDialog_Title");
        private string LetterDialog => L10N.Localize("ContactDialog_TitleLetter");
        private string LetterDescriptionDialog => L10N.Localize("ContactDialog_DescriptionLetter");
        private string PhoneDialog => L10N.Localize("ContactDialog_TitlePhone");
        private string PhoneDescriptionDialog => L10N.Localize("ContactDialog_DescriptionPhone");

        private void SetL10NResources()
        {
            LocationResources.Add("Title", TitleDialog);
            LocationResources.Add("TitleLetter", LetterDialog);
            LocationResources.Add("DescriptionLetter", LetterDescriptionDialog);
            LocationResources.Add("TitlePhone", PhoneDialog);
            LocationResources.Add("DescriptionPhone", PhoneDescriptionDialog);
        }

        #endregion
    }
}
