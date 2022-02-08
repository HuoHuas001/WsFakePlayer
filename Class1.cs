using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSR;
using WebSocketSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WsFakePlayer
{
    public class Class1
    {
		public static int FakePlayerNum = 0;

		public static string getType(int num)
        {
			string type;
			switch (num)
            {
                case 0:
					type = "连接中";
					break;
				case 1:
					type = "已连接";
					break;
				case 2:
					type = "断开连接中";
					break;
				case 3:
					type = "已断开连接";
					break;
				case 4:
					type = "重新连接中";
					break;
				case 5:
					type = "停止中";
					break;
				case 6:
					type = "已停止";
					break;
				default:
					type = "未知";
					break;
			}

			return type;
        }
        public static void init(MCCSAPI api)
        {
			string text = System.IO.File.ReadAllText(@"plugins/WsFakePlayer/config.json");
			JObject config = JObject.Parse(text); 
			string webPath = config["url"].ToString();
			WebSocket ws = new WebSocket(webPath);
			//建立连接
			ws.Connect();

			//获取
			ws.Send("{\"type\":\"list\"}");

			//OnMessage消息
			ws.OnMessage += (sender, e) =>
			{
				string msg = e.ToString();
				JObject jos = JObject.Parse(msg);
				JObject jo = (JObject)jos["data"];
				string packetType = jos["type"].ToString();
                switch (packetType)
                {
					case "list":
						JArray jArray = (JArray)jos["data.list"];
						int length = jArray.Count;
						Class1.FakePlayerNum = length;
						break;

					case "add":
						string name = jo["name"].ToString();
						string reason = jo["reson"].ToString();
						Boolean success = (bool)jo["success"];
                        if (success)
                        {
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\""+name+" added.\"}]}");
                        }
                        else
                        {
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + name + " failed. reason:"+reason+"\"}]}");
						}
						break;
					case "remove":
						string rname = jo["name"].ToString();
						string rreason = jo["reson"].ToString();
						Boolean rsuccess = (bool)jo["success"];
						if (rsuccess)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + rname + " added.\"}]}");
						}
						else
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + rname + " failed. reason:" + rreason + "\"}]}");
						}
						break;
					case "getState":
						string sname = jo["name"].ToString();
						string sreason = jo["reson"].ToString();
						Boolean ssuccess = (bool)jo["success"];
						int state = (int)jo["state"];
						if (ssuccess)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + sname + ':'+getType(state)+"\"}]}");
						}
						else
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + sname + " failed. reason:" + sreason + "\"}]}");
						}
						break;
					case "getState_all":
						JObject playersData = (JObject)jo["playersData"];
						string[] values = playersData.Properties().Select(item => item.Value.ToString()).ToArray();
						foreach (string plname in values) {
							JObject plo = (JObject)playersData[plname];
							string Pstate = getType((int)plo["state"]);
							Boolean acc = (bool)plo["allowChatControl"];
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\""+plname+" state:"+Pstate+ " allowChatControl:"+acc.ToString()+"\"}]}");
						}
						break;
					case "connect":
						string cname = jo["name"].ToString();
						string creason = jo["reson"].ToString();
						Boolean csuccess = (bool)jo["success"];
						if (csuccess)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + cname + " connected.\"}]}");
						}
						else
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + cname + " failed. reason:" + creason + "\"}]}");
						}
						break;
					case "disconnect":
						string dname = jo["name"].ToString();
						string dreason = jo["reson"].ToString();
						Boolean dsuccess = (bool)jo["success"];
						if (dsuccess)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + dname + " connected.\"}]}");
						}
						else
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + dname + " failed. reason:" + dreason + "\"}]}");
						}
						break;
					case "setChatControl":
						string setname = jo["name"].ToString();
						string setreason = jo["reson"].ToString();
						Boolean setsuccess = (bool)jo["success"];
						if (setsuccess)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + setname + " connected.\"}]}");
						}
						else
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + setname + " failed. reason:" + setreason + "\"}]}");
						}
						break;
					case "remove_all":
						JArray removeJArray = (JArray)jos["data.list"];
						foreach(string removename in removeJArray) {
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + removename + " removed.\"}]}");
						};
						break;
					case "connect_all":
						JArray connectJArray = (JArray)jos["data.list"];
						foreach (string connectname in connectJArray)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + connectname + " removed.\"}]}");
						};
						break;
					case "disconnect_all":
						JArray disconnectJArray = (JArray)jos["data.list"];
						foreach (string disconnectname in disconnectJArray)
						{
							api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + disconnectname + " removed.\"}]}");
						};
						break;
				}
			};

			//玩家输入指令
			api.addBeforeActListener("onInputCommand", x =>
			{
				var e = BaseEvent.getFrom(x) as InputCommandEvent;
				string[] Nocmds = { "list", "getState_all", "disconnect_all", "connect_all","remove_all"};
				if (e == null) return true;
                if (e.RESULT)
                {
					string cmds = e.cmd;
                    if (Nocmds.Contains(cmds.Split(' ')[1]))
                    {
						ws.Send("{\"type\":\""+cmds.Split(' ')[1]+ "\"}");
						if(cmds.Split(' ')[1] == "remove_all")
                        {
							ws.Send("{\"type\":\"list\"}");
						}
                    }
                    else
                    {
						string[] cmdp = cmds.Split(' ');
						switch (cmds.Split(' ')[1])
                        {
							case "add":
								if(cmdp[1] != null && cmdp[2] != null && cmdp[3] != null) {
									string j = string.Format('{'+"\"type\": \"add\",\"data\": "+'{'+"\"name\": \"{0}\",\"skin\": \"{1}\",\"allowChatControl\": {2}"+"}}",cmdp[1],cmdp[2],cmdp[3]);
									if(Class1.FakePlayerNum < (int)config["max"])
                                    {
										ws.Send(j);
										ws.Send("{\"type\":\"list\"}");
									}
									else
                                    {
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"FakePlayer count max.\"}]}");
									}
									
                                }
                                else
                                {
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
                                }
								break;

							case "remove":
								if (cmdp[1] != null)
								{
									string j = "{\"type\": \"remove\",\"data\": {\"name\": \"" + cmdp[1] + "\"}}";
									if (Class1.FakePlayerNum < (int)config["max"])
									{
										ws.Send(j);
										ws.Send("{\"type\":\"list\"}");
									}
									else
									{
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"FakePlayer count max.\"}]}");
									}
								}
								else
								{
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
								}
								break;

							case "getState":
								if (cmdp[1] != null)
								{
									string j = "{\"type\": \"getState\",\"data\": {\"name\": \"" + cmdp[1] + "\"}}";
									ws.Send(j);
								}
								else
								{
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
								}
								break;

							case "disconnect":
								if (cmdp[1] != null)
								{
									string j = "{\"type\": \"disconnect\",\"data\": {\"name\": \"" + cmdp[1] + "\",\"success\": true,\"reason\": \"\"}}";
									ws.Send(j);
								}
								else
								{
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
								}
								break;

							case "connect":
								if (cmdp[1] != null)
								{
									string j = "{\"type\": \"connect\",\"data\": {\"name\": \"" + cmdp[1] + "\"}}";
									ws.Send(j);
								}
								else
								{
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
								}
								break;

							case "setChatControl":
								if (cmdp[1] != null && cmdp[2] != null)
								{
									string j = "{\"type\": \"setChatControl\",\"data\": {\"name\": \"" + cmdp[1] + "\",\"allowChatControl\": "+cmdp[2]+"}}";
									ws.Send(j);
								}
								else
								{
									api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
								}
								break;

						}
                    }

                }
                return true;
			});
		}

		
    }
}

namespace CSR
{
	partial class Plugin
	{
		public static void onStart(MCCSAPI api)
		{
			// TODO 此接口为必要实现
			WsFakePlayer.Class1.init(api);
			api.setCommandDescribe("fp", "FakePlayer API");
			
			Console.WriteLine("[WsFakePlayer] Loaded.");
		}
	}
}