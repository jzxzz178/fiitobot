﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using TL;
using Yandex.Cloud.Mdb.Elasticsearch.V1;

namespace fiitobot.Services.Commands
{
    public class
        SpasibkaStartCommandHandler : IChatCommandHandler // self recovery: при ошибке скидывать в начальный стейт
    {
        private readonly IPresenter presenter;
        private readonly IBotDataRepository botDataRepo;
        private readonly IContactDetailsRepo contactDetailsRepo;

        public SpasibkaStartCommandHandler(IPresenter presenter, IBotDataRepository botDataRepo,
            IContactDetailsRepo contactDetailsRepo)
        {
            this.presenter = presenter;
            this.botDataRepo = botDataRepo;
            this.contactDetailsRepo = contactDetailsRepo;
        }

        public string Command => "/spasibka";
        public ContactType[] AllowedFor => ContactTypes.AllNotExternal;

        public async Task HandlePlainText(string text, long fromChatId, Contact sender, bool silentOnNoResults = false)
        {
            // из IContactDetailsRepo.FindBy(ID) можно найти ContactDetails человека
            // с помощью S3ContactsDetailsRepo.Save(ContactDetails) можно сохранить ContactDetails человека

            // await presenter.Say(text, fromChatId);
            var senderDetails = contactDetailsRepo.FindById(sender.Id).Result;
            try
            {
                var query = text.Split(" ")[1];
                if (!long.TryParse(query, out var receiverId))
                {
                    senderDetails.DialogState = new DialogState();
                }
                else
                {
                    var receiver = contactDetailsRepo.FindById(receiverId).Result;
                    HandleCurrentState(receiver, senderDetails, fromChatId);
                }

                await contactDetailsRepo.Save(senderDetails);
            }
            catch (Exception e)
            {
                senderDetails.DialogState = new DialogState();
                throw;
            }
        }

        private async void HandleCurrentState(ContactDetails receiver, ContactDetails sender, long fromChatId)
        {
            try
            {
                sender.DialogState.State = State.WaitingForContent;
                sender.DialogState.Receiver = receiver;
            }
            catch (Exception e)
            {
                sender.DialogState = new DialogState();
                throw;
            }

            await presenter.Say("Напишите текст спасибки", fromChatId);
        }

        public async void ResetSpasibkaState()
        {

        }
    }
}