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
		public static bool tellraw(MCCSAPI api,string name,string text)
        {
			if(name == "@a")
            {
				return api.runcmd("tellraw @a {\"rawtext\":[{\"text\":\"" + text + "\"}]}");
			}
            else
            {
				return api.runcmd("tellraw \"" + name + "\" {\"rawtext\":[{\"text\":\"" + text + "\"}]}");
			}
			
        }

        public static void init(MCCSAPI api)
        {
			int FakePlayerNum = 0;
			string text = System.IO.File.ReadAllText(@"plugins/WsFakePlayer/config.json");
			JObject config = JObject.Parse(text); 
			string webPath = config["url"].ToString();
			Console.WriteLine(webPath);
			WebSocket ws = new WebSocket(webPath);
			bool firstConnect = true;
			//建立连接
			ws.Connect();

			//获取
			ws.Send("{\"type\":\"list\"}");

			//OnMessage消息
			ws.OnMessage += (sender, e) =>
			{
				string msg = e.Data;
				//转化json
				JObject Json_Msg = JObject.Parse(msg);
				string[] jsonin = Json_Msg.Properties().Select(item => item.Name.ToString()).ToArray();
				if (Json_Msg != null)
                {
					//Type相应
                    if (jsonin.Contains("type"))
                    {
						string packetType = Json_Msg["type"].ToString();
						switch (packetType)
						{
							case "list":
								JArray PlayerList = JArray.Parse(Json_Msg["data"]["list"].ToString());
								FakePlayerNum = PlayerList.Count;

								if (firstConnect)
								{
									Console.WriteLine("[WsFakePlayer] Connect to server success.");
									firstConnect = false;
								}
								else
								{
									string plname = "";
									foreach(string player in PlayerList)
                                    {
										plname += player + ' ';
                                    }
									tellraw(api, "@a", "FakePlayerList:" + plname);
								}
								break;
							case "add":
							case "remove":
							case "connect":
							case "setChatControl":
								JObject addData = JObject.Parse(Json_Msg["data"].ToString());
								string name = addData["name"].ToString();
								Boolean success = (bool)addData["success"];
								string reason = addData["reason"].ToString();
                                if (success)
                                {
									tellraw(api, "@a", name + " " +packetType + " success.");
                                }
                                else
                                {
									tellraw(api, "@a", name + " " + packetType + " failed reason:"+reason+'.');
								}
								break;
							case "getState":
								JObject stateData = JObject.Parse(Json_Msg["data"].ToString());
								string statename = stateData["name"].ToString();
								Boolean statesuccess = (bool)stateData["success"];
								string statereason = stateData["reason"].ToString();
								string nowState = getType((int)stateData["state"]);
								if (statesuccess)
								{
									tellraw(api, "@a", statename + " " + packetType + "success. state:"+nowState);
								}
								else
								{
									tellraw(api, "@a", statename + " " + packetType + "failed reason:" + statereason + '.');
								}
								break;
							case "getState_all":
								JObject stateList = JObject.Parse(Json_Msg["data"]["playersData"].ToString());
								string[] stateObject = stateList.Properties().Select(item => item.Name.ToString()).ToArray();
								foreach(string pName in stateObject)
                                {
									JObject plNow = JObject.Parse(stateList[pName].ToString());
									tellraw(api, "@a", pName + " state:" + getType((int)plNow["state"]) + " allowChatControl:"+ plNow["allowChatControl"].ToString());
                                }
								break;
							case "remove_all":
							case "disconnect_all":
							case "connect_all":
								JArray allList = JArray.Parse(Json_Msg["data"]["list"].ToString());
								string plName = "";
								string typeName = packetType.Replace("_all","");
								foreach (string player in allList)
								{
									plName += player + ' ';
								}
								tellraw(api, "@a", typeName+" List:" + plName);
								break;
							default:
								Console.WriteLine("[WsFakePlayer] Unkown packet.");
								break;
						}
					}
					//Event事件
					else if(jsonin.Contains("event"))
                    {
						JObject dataObject = JObject.Parse(Json_Msg["data"].ToString());
						string packetType = Json_Msg["event"].ToString();
						switch (packetType)
                        {
							case "add":
							case "connect":
							case "disconnect":
								string name = dataObject["name"].ToString();
								string state = getType((int)dataObject["state"]);
								tellraw(api, "@a", name + " " + packetType + " state:" + state);
								break;
							case "remove":
								string removeName = dataObject["name"].ToString();
								tellraw(api, "@a", removeName + " " + packetType+"d.");
								break;
							default:
								Console.WriteLine("[WsFakePlayer] Unkown packet.");
								break;
                        }
                    }
					
                }
			};

			//玩家输入指令
			api.addBeforeActListener(EventKey.onInputCommand, x => {
				var e = BaseEvent.getFrom(x) as InputCommandEvent;
				string[] Nocmds = { "list", "getState_all", "disconnect_all", "connect_all", "remove_all" };
				if (e != null)
				{
					string cmds = e.cmd;
					string[] cmdArray = cmds.Split(' ').ToArray();
					if (cmdArray[0] == "/fp" && cmdArray.Length >= 2)
					{
                        if (true) { 
							string[] cmdp = cmds.Split(' ').ToArray();
							switch (cmdp[1])
							{
								case "add":
									if (cmdp[2] != null && cmdp[3] != null && cmdp[4] != null)
									{
										string j = "{\"type\": \"add\",\"data\": {\"name\": \""+ cmdp[2] + "\",\"skin\": \""+ cmdp[3] + "\",\"allowChatControl\": "+ cmdp[4]+ "}}";
										if (FakePlayerNum < (int)config["max"])
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
									if (cmdp[2] != null)
									{
										string j = "{\"type\": \"remove\",\"data\": {\"name\": \"" + cmdp[2] + "\"}}";
										ws.Send(j);
										ws.Send("{\"type\":\"list\"}");
									}
									break;

								case "getState":
									if (cmdp[2] != null)
									{
										string j = "{\"type\": \"getState\",\"data\": {\"name\": \"" + cmdp[2] + "\"}}";
										ws.Send(j);
									}
									else
									{
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
									}
									break;

								case "disconnect":
									if (cmdp[2] != null)
									{
										string j = "{\"type\": \"disconnect\",\"data\": {\"name\": \"" + cmdp[2] + "\",\"success\": true,\"reason\": \"\"}}";
										ws.Send(j);
									}
									else
									{
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
									}
									break;

								case "connect":
									if (cmdp[2] != null)
									{
										string j = "{\"type\": \"connect\",\"data\": {\"name\": \"" + cmdp[2] + "\"}}";
										ws.Send(j);
									}
									else
									{
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
									}
									break;

								case "setChatControl":
									if (cmdp[2] != null && cmdp[3] != null)
									{
										string j = "{\"type\": \"setChatControl\",\"data\": {\"name\": \"" + cmdp[2] + "\",\"allowChatControl\": " + cmdp[3] + "}}";
										ws.Send(j);
									}
									else
									{
										api.runcmd("tellraw \"" + e.playername + "\" {\"rawtext\":[{\"text\":\"The args error\"}]}");
									}
									break;
								default:
									ws.Send("{\"type\":\"" + cmdp[1] + "\"}");
									break;

							}
						}
						return false;
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