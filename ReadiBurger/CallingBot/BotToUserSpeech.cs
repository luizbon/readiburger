using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;

namespace ReadiBurger.CallingBot
{
    public class BotToUserSpeech : IBotToUser
    {
        private readonly Action<string, bool> _callback;

        private readonly IMessageActivity _toBot;
        public BotToUserSpeech(IMessageActivity toBot, Action<string, bool> callback)
        {
            SetField.NotNull(out _toBot, nameof(toBot), toBot);
            _callback = callback;
        }

        public IMessageActivity MakeMessage()
        {
            return _toBot;
        }

        public async Task PostAsync(IMessageActivity message, CancellationToken cancellationToken = default(CancellationToken))
        {
            _callback(message.Text, message.InputHint == InputHints.ExpectingInput);
            if (message.Attachments?.Count > 0)
                _callback(ButtonsToText(message.Attachments), false);
        }

        private static string ButtonsToText(IEnumerable<Attachment> attachments)
        {
            var cardAttachments = attachments?.Where(attachment => attachment.ContentType.StartsWith("application/vnd.microsoft.card")).ToList();
            var builder = new StringBuilder();
            if (cardAttachments == null || !cardAttachments.Any()) return builder.ToString();
            {
                builder.AppendLine();
                foreach (var attachment in cardAttachments)
                {
                    var type = attachment.ContentType.Split('.').Last();

                    if (type != "hero" && type != "thumbnail") continue;

                    var card = (HeroCard)attachment.Content;

                    if (!string.IsNullOrEmpty(card.Title))
                    {
                        builder.AppendLine(card.Title);
                    }

                    if (!string.IsNullOrEmpty(card.Subtitle))
                    {
                        builder.AppendLine(card.Subtitle);
                    }

                    if (!string.IsNullOrEmpty(card.Text))
                    {
                        builder.AppendLine(card.Text);
                    }

                    if (card.Buttons == null) continue;

                    foreach (var button in card.Buttons)
                    {
                        builder.AppendLine(button.Title);
                    }
                }
            }
            return builder.ToString();
        }

    }
}