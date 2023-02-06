using System;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace OfflineRaidHelper
{
	class Program
	{
		static string saveDirectory = AppDomain.CurrentDomain.BaseDirectory;
		static string siteSource;
		static string url = "";
		static List<string> names = new List<string>();
		static string serverName;
		static string[] players;
		static List<string> outputBuffer = new List<string>();
		static string info = "";
		static int updateTime = 10;
		static int onlineTargets = 0;
		static void Main(string[] args)
		{
			Thread thread1 = new Thread(Update);
			thread1.Start();

			//Thread bufferCleaner = new Thread(UpdateBuffer);
			//bufferCleaner.Start();

			outputBuffer.Add("type \"server <server id>\" to select a server");
			UpdateBuffer();
			while (true)
			{
				string command = Console.ReadLine();
				if (command.Contains("server"))
				{
					try
					{
						url = command.Substring(7);
						outputBuffer.Add("Server " + url + " added");
					}
					catch (Exception e)
					{ outputBuffer.Add("Invalid argument use: server <id>"); }
					UpdateBuffer();
				}
				else if (command.Contains("target add"))
				{
					try
					{
						names.Add(command.Substring(11));
						outputBuffer.Add("Target " + command.Substring(11) + " added");
					}
					catch { outputBuffer.Add("Invalid argument use: target add <player name>"); }

					UpdateBuffer();
				}
				else if (command.Contains("target remove"))
				{
					try
					{
						names.Remove(command.Substring(14));
						outputBuffer.Add("Target " + command.Substring(14) + " removed");
					}
					catch { outputBuffer.Add("Invalid argument use: target remove <player name>"); }
					UpdateBuffer();
				}
				else if (command.Contains("update"))
				{
					try
					{
						updateTime = Int32.Parse(command.Substring(7));
						if (updateTime < 1)
							updateTime = 1;
						outputBuffer.Add("Update rate: " + updateTime.ToString());
					}
					catch { outputBuffer.Add("Invalid argument use: update <sec>"); }
					UpdateBuffer();
				}
				else if (command.Contains("load"))
				{
					try
					{
						string fileName = command.Substring(5)+".txt";
						if (File.Exists(saveDirectory + fileName))
						{
							names.Clear();
							outputBuffer.Add("File: " + fileName + " loaded!");
							string[] lines = File.ReadAllLines(saveDirectory + fileName);
							for (int i = 0; i < lines.Length; i++)
							{
								if (lines[i].Contains("server"))
								{
									try
									{
										url = lines[i].Substring(7);
										outputBuffer.Add("Server " + url + " added");
									}
									catch (Exception e)
									{ outputBuffer.Add("Invalid argument use: server <id>"); }
									UpdateBuffer();
								}
								else if (lines[i].Contains("targets"))
								{
									string[] tempNames = lines[i].Split('=');
									tempNames = tempNames.Skip(1).ToArray();
									for (int j = 0; j < tempNames.Length; j++)
									{
										try
										{
											names.Add(tempNames[j]);
											outputBuffer.Add("Target " + tempNames[j] + " added");
										}
										catch { outputBuffer.Add("Invalid argument use: target add <player name>"); }
									}

								}
							}
						}
						else
							outputBuffer.Add("File doesnt exist!");
					}
					catch { outputBuffer.Add("Invalid argument use: load <file name>"); }
					UpdateBuffer();
				}
				else if (command.Contains("save"))
				{
					try
					{
						string fileName = command.Substring(5) + ".txt";
						if (!File.Exists(saveDirectory + fileName))
						{
							FileStream fil = File.Create(saveDirectory + fileName);
							Thread.Sleep(1000);
							fil.Flush();
							fil.Close();
						}
						string temp = "";
						for (int i = 0; i < names.Count; i++)
							temp = temp + "=" + names[i];
						File.WriteAllText(saveDirectory + fileName, "server=" + url + "\ntargets" + temp);
					}
					catch (Exception e)
					{
						outputBuffer.Add(e.Message.ToString());
					}
					UpdateBuffer();
				}
				else if (command == "targets" && names != null)
				{
					string temp = "";
					for (int i = 0; i < names.Count; i++)
						temp = temp + names[i] + "\n";
					outputBuffer.Add("Targets:\n" + temp);
					UpdateBuffer();

				}
				else if (command == "players" && players != null)
				{
					string temp = "";
					for (int i = 0; i < players.Length; i++)
						temp = temp + players[i] + "\n";
					outputBuffer.Add("Online players:\n" + temp);
					UpdateBuffer();
				}
				else if (command == "help")
				{
					outputBuffer.Add("server <id> - select server\nplayers - shows all online players\ntargets - shows all targets\ntarget add <player name> - adds target\ntarget remove <player name> - removes target\nsave <file name> - saves config\nload <file name> - loads config\nupdate <sec> (default: 10) - how often players update");
					UpdateBuffer();
				}
				else
				{
					outputBuffer.Add("Invalid command use \"help\" to show all commands");
					UpdateBuffer();
				}
				UpdateManual();
			}
		}
		static void UpdateBuffer()
		{
			Console.Clear();
			if (outputBuffer.Count > 10)
				outputBuffer.RemoveAt(0);
			for (int i = 0; i < outputBuffer.Count; i++)
			{
				Console.WriteLine(outputBuffer[i]);
			}
			Console.WriteLine(info);
		}
		static void Update()
		{
			WebClient myWebClient = new WebClient();
			while (true)
			{
				if (url != "")
				{
					try
					{
						siteSource = myWebClient.DownloadString("https://www.battlemetrics.com/servers/rust/" + url); //Getting battlemetrics source 
						if (siteSource.Length < 10)
						{
							outputBuffer.Add("Invalid server.");
							UpdateBuffer();
						}
					}
					catch (Exception e)
					{
						outputBuffer.Add("Invalid server.");
						UpdateBuffer();
					}
					if (siteSource != null)
					{
						//Getting server name from source
						try
						{
							serverName = siteSource.Substring(siteSource.IndexOf("\"game_id\":\"rust\",\"name\":") + 25).Remove(siteSource.Substring(siteSource.IndexOf("\"game_id\":\"rust\",\"name\":") + 25).IndexOf("address") - 3);

							//getting players part from source
							string playersSource = siteSource.Remove(siteSource.IndexOf("Most Time Played"));
							playersSource = playersSource.Substring(playersSource.IndexOf("Play time"));

							//splitting each player's source into separate strings
							players = playersSource.Split(new string[] { "/players/" }, StringSplitOptions.None);
							players = players.Skip(1).ToArray();


							onlineTargets = 0;
							string temp = "";
							for (int i = 0; i < players.Length; i++)
							{
								//Filtering only names
								players[i] = players[i].Substring(players[i].IndexOf("\">") + 2);
								players[i] = players[i].Remove(players[i].IndexOf("</a>"));
								if (names.Contains(players[i]))
								{
									temp = temp + players[i] + "\n";
									onlineTargets++;
								}
							}
							info = "-------------------------------------------------------------\n" + serverName + "\n(" + onlineTargets.ToString() + "/" + players.Length + ")" + "Online targets:\n" + temp;
						}
						catch (Exception e)
						{
							outputBuffer.Add("Invalid server.");
						}
						UpdateBuffer();
					}
				}
				Thread.Sleep(updateTime * 1000);
			}
		}
		static void UpdateManual()
		{
			WebClient myWebClient = new WebClient();
			if (url != "")
			{
				try
				{
					siteSource = myWebClient.DownloadString("https://www.battlemetrics.com/servers/rust/" + url); //Getting battlemetrics source 
					if (siteSource.Length < 10)
					{
						outputBuffer.Add("Invalid server.");
						UpdateBuffer();
					}
				}
				catch (Exception e)
				{
					outputBuffer.Add("Invalid server.");
					UpdateBuffer();
				}
				if (siteSource != null)
				{
					//Getting server name from source
					try
					{
						serverName = siteSource.Substring(siteSource.IndexOf("\"game_id\":\"rust\",\"name\":") + 25).Remove(siteSource.Substring(siteSource.IndexOf("\"game_id\":\"rust\",\"name\":") + 25).IndexOf("address") - 3);

						//getting players part from source
						string playersSource = siteSource.Remove(siteSource.IndexOf("Most Time Played"));
						playersSource = playersSource.Substring(playersSource.IndexOf("Play time"));

						//splitting each player's source into separate strings
						players = playersSource.Split(new string[] { "/players/" }, StringSplitOptions.None);
						players = players.Skip(1).ToArray();


						onlineTargets = 0;
						string temp = "";
						for (int i = 0; i < players.Length; i++)
						{
						//Filtering only names
							players[i] = players[i].Substring(players[i].IndexOf("\">") + 2);
							players[i] = players[i].Remove(players[i].IndexOf("</a>"));
							if (names.Contains(players[i]))
								temp = temp + players[i] + "\n";
						}
						info = "-------------------------------------------------------------\n" + serverName + "\n(" + onlineTargets.ToString() + "/" + players.Length + ")" + "Online targets:\n" + temp;
					}
					catch (Exception e)
					{
						outputBuffer.Add("Invalid server.");
					}
					UpdateBuffer();
				}
			}
		}
	}

}
