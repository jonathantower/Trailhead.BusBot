using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using ProtoBuf;
using transit_realtime;

namespace Trailhead.BusBot.Dialogs
{
	[Serializable]
	public class BusDialog : IDialog<object>
	{
		public Task StartAsync(IDialogContext context)
		{
			context.Wait(MessageReceivedAsync);
			return Task.CompletedTask;
		}

		private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
		{
			// return our reply to the user
			await context.PostAsync("Hello");
			
			context.Wait(MessageReceivedAsync);
		}

		private IMessageActivity HelpCommand(string receivedMsg, IDialogContext context)
		{
			var msg = context.MakeMessage();
			msg.Text = "Type one of these commands:\n\n" +
					   "list - lists all active buses\n\n" +
					   "9999 - any bus number to get the location of that bus\n\n" +
					   "help - to get this help message";
			return msg;
		}

		private IMessageActivity BusListingCommand(string receivedMsg, IDialogContext context)
		{
			var msg = context.MakeMessage();
			var req = WebRequest.Create("http://connect.ridetherapid.org/infopoint/GTFS-Realtime.ashx?&Type=VehiclePosition");
			var feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());
			msg.Text = "All buses: " + string.Join(", ",
				feed.entity.OrderBy(i => i.vehicle.vehicle.id).Select(i => i.vehicle.vehicle.label).ToArray());

			return msg;
		}

		private IMessageActivity BusStatusCommand(string receivedMsg, IDialogContext context)
		{
			var m = context.MakeMessage();
			var msg = "Could not find status of requested bus";
			var busNum = Regex.Match(receivedMsg, "[0-9]+").Value;

			var req = WebRequest.Create("http://connect.ridetherapid.org/infopoint/GTFS-Realtime.ashx?&Type=VehiclePosition");
			var feed = Serializer.Deserialize<FeedMessage>(req.GetResponse().GetResponseStream());

			var bus = feed.entity.FirstOrDefault(i => i.vehicle.vehicle.label == busNum);
			if (bus != null)
			{
				var p = bus.vehicle.position;
				var direction = GetDirection(bus);

				msg = $"Bus {busNum} is heading {direction} at {p.speed:0.0} MPH";

				m.Attachments.Add(new Attachment("image/png",
					$"https://maps.googleapis.com/maps/api/staticmap?center={p.latitude},{p.longitude}&markers=color:red%7C{p.latitude},{p.longitude}&zoom=15&size=400x300&key=", name: "map"));
			}

			m.Text = msg;


			return m;
		}

		private string GetDirection(FeedEntity bus)
		{
			var cardinalDirection = (Math.Round(bus.vehicle.position.bearing / 45.0) * 45) % 360.0;
			var directionName = "unknown direction";
			switch (cardinalDirection)
			{
				case 0:
					directionName = "north";
					break;
				case 45:
					directionName = "northeast";
					break;
				case 90:
					directionName = "east";
					break;
				case 135:
					directionName = "southeast";
					break;
				case 180:
					directionName = "south";
					break;
				case 225:
					directionName = "southwest";
					break;
				case 270:
					directionName = "west";
					break;
				case 315:
					directionName = "northwest";
					break;
			}
			return directionName;
		}
	}
}