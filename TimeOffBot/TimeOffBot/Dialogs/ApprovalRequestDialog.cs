using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace TimeOffBot.Dialogs
{
    [Serializable]
    public class ApprovalRequestDialog : IDialog<object>
    {
    
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Hello, @User has requested time off. Will you Approve?");
            context.Done(String.Empty);
            //context.Wait(this.MessageReceivedAsync);
        }

        //public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        //{
        //    if ((await result).Text == "done")
        //    {
        //        await context.PostAsync("Great, back to the original conversation!");
        //        context.Done(String.Empty); //Finish this dialog
        //    }
        //    else
        //    {
        //        await context.PostAsync("I'm still on the survey until you type \"done\"");
        //        context.Wait(MessageReceivedAsync); //Not done yet
        //    }
        //}
    }
}