using LINEBot.Models;
using LineMessagingAPISDK;
using LineMessagingAPISDK.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LINEBot.Controllers
{
    [RoutePrefix("webhook")]
    public class LineMessagesController : ApiController
    {
        private Models.DatabaseContext db = new Models.DatabaseContext();

        [Route]
        public async Task<HttpResponseMessage> Post(HttpRequestMessage request)
        {
            if (await VaridateSignature(request) == "error")
                return Request.CreateResponse(HttpStatusCode.BadRequest);

            var channelToken = await VaridateSignature(request);

            Activity activity = JsonConvert.DeserializeObject<Activity>
                (await request.Content.ReadAsStringAsync());

            foreach (Event lineEvent in activity.Events)
            {
                LineMessageHandler handler = new LineMessageHandler(lineEvent, channelToken);

                Profile profile = await handler.GetProfile(lineEvent.Source.UserId);

                switch (lineEvent.Type)
                {
                    case LineMessagingAPISDK.Models.EventType.Beacon:
                        await handler.HandleBeaconEvent();
                        break;
                    case LineMessagingAPISDK.Models.EventType.Follow:
                        await handler.HandleFollowEvent();
                        break;
                    case LineMessagingAPISDK.Models.EventType.Join:
                        await handler.HandleJoinEvent();
                        break;
                    case LineMessagingAPISDK.Models.EventType.Leave:
                        await handler.HandleLeaveEvent();
                        break;
                    case LineMessagingAPISDK.Models.EventType.Message:
                        Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
                        switch (message.Type)
                        {
                            case MessageType.Text:
                                await handler.HandleTextMessage();
                                break;
                            case MessageType.Audio:
                            case MessageType.Image:
                            case MessageType.Video:
                                await handler.HandleMediaMessage();
                                break;
                            case MessageType.Sticker:
                                await handler.HandleStickerMessage();
                                break;
                            case MessageType.Location:
                                await handler.HandleLocationMessage();
                                break;
                        }
                        break;
                    case LineMessagingAPISDK.Models.EventType.Postback:
                        await handler.HandlePostbackEvent();
                        break;
                    case LineMessagingAPISDK.Models.EventType.Unfollow:
                        await handler.HandleUnfollowEvent();
                        break;
                }
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // 需要雜湊驗證
        private async Task<string> VaridateSignature(HttpRequestMessage request)
        {
            var headerHash = Request.Headers.GetValues("X-Line-Signature").First();
            var secret = "error";

            IQueryable<Bot> results = db.Bots.Select(x => x);
            
            foreach (Bot rs in results)
            {
                var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(rs.ChannelSecret));
                var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(await request.Content.ReadAsStringAsync()));
                var contentHash = Convert.ToBase64String(computeHash);
                if (contentHash == headerHash)
                {
                    secret = rs.ChannelSecret;
                    break;
                }
            }

            return secret;
        }
    }

    public class LineMessageHandler
    {
        private Event lineEvent;
        private string channelToken;
        private LineClient lineClient = null;
        private Models.DatabaseContext db = new Models.DatabaseContext();

        public LineMessageHandler(Event lineEvent, string channelToken)
        {
            this.lineEvent = lineEvent;
            this.channelToken = channelToken;
        }

        // 事件：Beacon
        public async Task HandleBeaconEvent()
        {
            List<Models.Message> messages = db.Messages.Where(x => x.Event == 1).ToList();
        }

        //　事件：追蹤
        public async Task HandleFollowEvent()
        {
            List<Models.Message> messages = db.Messages.Where(x => x.Event == 2).ToList();
        }

        // 事件：加入
        public async Task HandleJoinEvent()
        {
            List<Models.Message> messages = db.Messages.Where(x => x.Event == 3).ToList();
        }

        // 事件：離開
        public async Task HandleLeaveEvent()
        {
            List<Models.Message> messages = db.Messages.Where(x => x.Event == 4).ToList();
        }

        public async Task HandlePostbackEvent()
        {
            string reply;
            // Handle DateTimePicker postback
            if (lineEvent.Postback?.Params != null)
            {
                var dateTime = lineEvent.Postback?.Params;
                reply = $"DateTime: {dateTime.DateTime}, Date: {dateTime.Date}, Time: {dateTime.Time}";
            }
            else
            {
                reply = lineEvent.Postback.Data;
            }
            await Reply(new TextMessage(reply));
        }

        // 事件：取消追蹤
        public async Task HandleUnfollowEvent()
        {
            List<Models.Message> messages = db.Messages.Where(x => x.Event == 7).ToList();
        }

        public async Task<Profile> GetProfile(string mid)
        {
            lineClient = new LineClient(channelToken);
            return await lineClient.GetProfile(mid);
        }

        // 事件：文字
        public async Task HandleTextMessage()
        {
            var textMessage = JsonConvert.DeserializeObject<TextMessage>(lineEvent.Message.ToString());
            Message replyMessage = null;
            List<Models.Message> messages = db.Messages.Where(x => x.KeyWord.Contains(textMessage.Text) && x.Event == 5).ToList();
            if (messages != null)
            {
                Dictionary<int, int> matchMessages = null;
                List<int> messageIds = null;
                List<Models.Message> frequentMessages = null;
                
                foreach (var message in messages)
                {
                    string[] keywords = message.KeyWord.Split(',');
                    int count = 0;
                    foreach (var keyword in keywords)
                        if (textMessage.Text.Contains(keyword)) matchMessages.Add(message.MessageId, count++);
                }
                // 排序
                var sortMessages = from objDic in matchMessages orderby objDic.Value descending select objDic;
                foreach (KeyValuePair<int, int> kvp in sortMessages)
                    messageIds.Add(kvp.Key);
                frequentMessages = db.Messages.Where(x => messageIds.Contains(x.MessageId)).ToList();

                // 文字
                if (frequentMessages.First().Type == 1)
                {
                    replyMessage = new TextMessage(frequentMessages.First().Text);
                }
                // 貼圖
                else if (frequentMessages.First().Type == 2)
                {
                    // https://devdocs.line.me/files/sticker_list.pdf
                    var stickerMessage = JsonConvert.DeserializeObject<StickerMessage>(lineEvent.Message.ToString());
                    replyMessage = new StickerMessage(frequentMessages.First().STKId, frequentMessages.First().STKPKGId);
                }
                // 圖片
                else if (frequentMessages.First().Type == 3)
                {
                    replyMessage = new ImageMessage(frequentMessages.First().ImageUrl, frequentMessages.First().Url);
                }
                // 影片
                else if (frequentMessages.First().Type == 4)
                {
                    replyMessage = new ImageMessage(frequentMessages.First().ImageUrl, frequentMessages.First().Url);
                }
                // 聲音
                else if (frequentMessages.First().Type == 5)
                {
                    replyMessage = new ImageMessage(frequentMessages.First().ImageUrl, frequentMessages.First().Url);
                }
                // 位置
                else if (frequentMessages.First().Type == 6)
                {
                    replyMessage = new LocationMessage(
                        frequentMessages.First().Title,
                        frequentMessages.First().Address,
                        frequentMessages.First().Latitude,
                        frequentMessages.First().Longitude);
                }
                // 圖片地圖
                else if (frequentMessages.First().Type == 7)
                {
                    // 待研究
                    var url = HttpContext.Current.Request.Url;
                    var imageUrl = $"{url.Scheme}://{url.Host}:{url.Port}/images/githubicon";
                    List<ImageMapAction> actions = new List<ImageMapAction>();
                    actions.Add(new UriImageMapAction("http://github.com", new ImageMapArea(0, 0, 520, 1040)));
                    actions.Add(new MessageImageMapAction("I love LINE!", new ImageMapArea(520, 0, 520, 1040)));
                    replyMessage = new ImageMapMessage(imageUrl, "GitHub", new BaseSize(1040, 1040), actions);
                }
                // 模板：按鈕
                else if (frequentMessages.First().Type == 8)
                {
                    List<TemplateAction> actions = new List<TemplateAction>();
                    if (frequentMessages.First().Msg != "" || frequentMessages.First().Msg != null) actions.Add(new MessageTemplateAction("傳送訊息", frequentMessages.First().Msg));
                    if (frequentMessages.First().PostBack != "" || frequentMessages.First().PostBack != null) actions.Add(new PostbackTemplateAction("執行動作", frequentMessages.First().PostBack));
                    if (frequentMessages.First().Url != "" || frequentMessages.First().Url != null) actions.Add(new UriTemplateAction("查看全部", frequentMessages.First().Url));
                    ButtonsTemplate buttonsTemplate = new ButtonsTemplate(frequentMessages.First().ImageUrl, frequentMessages.First().Title, frequentMessages.First().Text, actions);

                    replyMessage = new TemplateMessage("Buttons", buttonsTemplate);
                }
                // 模板：確認
                else if (frequentMessages.First().Type == 9)
                {
                    List<TemplateAction> actions = new List<TemplateAction>();
                    actions.Add(new MessageTemplateAction("是", frequentMessages.First().Yes));
                    actions.Add(new MessageTemplateAction("否", frequentMessages.First().No));
                    ConfirmTemplate confirmTemplate = new ConfirmTemplate(frequentMessages.First().Title, actions);
                    replyMessage = new TemplateMessage("Confirm", confirmTemplate);
                }
                // 模板：輪播
                else if (frequentMessages.First().Type == 10)
                {
                    List<TemplateColumn> columns = new List<TemplateColumn>();
                    List<TemplateAction> actions = new List<TemplateAction>();

                    foreach (var message in frequentMessages.Where(x => x.Type == 10))
                    {
                        actions = null;
                        if (message.Msg != "" || message.Msg != null) actions.Add(new MessageTemplateAction("傳送訊息", message.Msg));
                        if (message.PostBack != "" || message.PostBack != null) actions.Add(new PostbackTemplateAction("執行動作", message.PostBack));
                        if (message.Url != "" || message.Url != null) actions.Add(new UriTemplateAction("查看全部", message.Url));

                        columns.Add(new TemplateColumn() { Title = message.Title, Text = message.Text, ThumbnailImageUrl = message.ImageUrl, Actions = actions });
                    }

                    CarouselTemplate carouselTemplate = new CarouselTemplate(columns);
                    replyMessage = new TemplateMessage("Carousel", carouselTemplate);
                }
                // 模板：圖片輪播
                else if (frequentMessages.First().Type == 11)
                {
                    List<ImageColumn> columns = new List<ImageColumn>();

                    foreach (var message in frequentMessages.Where(x => x.Type == 11))
                    {
                        UriTemplateAction action = new UriTemplateAction("查看全部", message.Url);
                        columns.Add(new ImageColumn(message.ImageUrl, action));
                    }

                    ImageCarouselTemplate carouselTemplate = new ImageCarouselTemplate(columns);
                    replyMessage = new TemplateMessage("Carousel", carouselTemplate);
                }
                // 模板：新增豐富菜單
                else if (frequentMessages.First().Type == 12)
                {
                    // Create Rich Menu
                    RichMenu richMenu = new RichMenu()
                    {
                        Size = new RichMenuSize(1686),
                        Selected = false,
                        Name = frequentMessages.First().Title,
                        ChatBarText = "touch me",
                        Areas = new List<RichMenuArea>()
                        {
                            new RichMenuArea()
                            {
                                Action = new PostbackTemplateAction("action=buy&itemid=123"),
                                Bounds = new RichMenuBounds(0, 0, 2500, 1686)
                            }
                        }
                    };


                    var richMenuId = await lineClient.CreateRichMenu(richMenu);
                    var image = new MemoryStream(File.ReadAllBytes(HttpContext.Current.Server.MapPath(@"~\Images\richmenu.PNG")));
                    // Upload Image
                    await lineClient.UploadRichMenuImage(richMenuId, image);
                    // Link to user
                    await lineClient.LinkRichMenuToUser(lineEvent.Source.UserId, richMenuId);
                }
                // 模板：刪除豐富菜單
                else
                {
                    var richMenuId = await lineClient.GetRichMenuIdForUser(lineEvent.Source.UserId);

                    await lineClient.UnlinkRichMenuToUser(lineEvent.Source.UserId);
                    await lineClient.DeleteRichMenu(richMenuId);
                }
            }
            else
            {
                replyMessage = new TextMessage("不好意思！我不懂您的意思...");
            }
            await Reply(replyMessage);
        }

        public async Task HandleMediaMessage()
        {
            Message message = JsonConvert.DeserializeObject<Message>(lineEvent.Message.ToString());
            // Get media from Line server.
            Media media = await lineClient.GetContent(message.Id);
            Message replyMessage = null;

            // Reply Image 
            switch (message.Type)
            {
                case MessageType.Image:
                case MessageType.Video:
                case MessageType.Audio:
                    replyMessage = new ImageMessage("https://cdn2.ettoday.net/images/1930/e1930790.jpg", "https://cdn2.ettoday.net/images/1930/e1930790.jpg");
                    break;
            }

            await Reply(replyMessage);
        }

        public async Task HandleStickerMessage()
        {
            // https://devdocs.line.me/files/sticker_list.pdf
            var stickerMessage = JsonConvert.DeserializeObject<StickerMessage>(lineEvent.Message.ToString());
            var replyMessage = new StickerMessage("1", "1");
            await Reply(replyMessage);
        }

        public async Task HandleLocationMessage()
        {
            var locationMessage = JsonConvert.DeserializeObject<LocationMessage>(lineEvent.Message.ToString());
            LocationMessage replyMessage = new LocationMessage(
                locationMessage.Title,
                locationMessage.Address,
                locationMessage.Latitude,
                locationMessage.Longitude);
            await Reply(replyMessage);
        }

        private async Task Reply(Message replyMessage)
        {
            try
            {
                await lineClient.ReplyToActivityAsync(lineEvent.CreateReply(message: replyMessage));
            }
            catch
            {
                await lineClient.PushAsync(lineEvent.CreatePush(message: replyMessage));
            }
        }
    }
}