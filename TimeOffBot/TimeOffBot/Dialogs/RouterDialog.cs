using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    // roots the conversation to the appropriate dialog
    public class RouterDialog : DispatchDialog
    {
        [RegexPattern(DialogMatches.TimeOffRequestMatch)]
        [ScorableGroup(1)]
        public async Task DoTimeOffRequest(IDialogContext context, IActivity activity)
        {
            context.Call(new TimeOffDialog(), this.EndDialog);
        }

        [RegexPattern(DialogMatches.SetApprovalChannelMatch)]
        [ScorableGroup(1)]
        public async Task SetApprovalChannel(IDialogContext context, IActivity activity)
        {
            context.Call(new SetApprovalChannelDialog(), this.EndDialog);
        }

        public async Task EndDialog(IDialogContext context, IAwaitable<object> result)
        {
            context.Done<object>(null);
        }

    }
}