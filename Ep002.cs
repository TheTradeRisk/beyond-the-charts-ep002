//==============================================================================
// Project:     	The Trade Risk - Beyond The Charts Episode 002 	           
// Name:        	EP002 													   
// Description: 	Is buying stocks hitting new 52-week highs a profitable trading strategy? 			   
// Author website:  https://www.thetraderisk.com							   
// Author contact:	contact@thetraderisk.com								   
//==============================================================================

#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.BeyondTheCharts
{
	public class Ep002 : Strategy
	{		
		private double startingBalance;
		private double currentBalance;
		private double realizedPnL;
		private double unrealizedPnL;
		private double entryPrice;
		private double exitPrice;
		private double netTrade;
		private double rMultiple;
		private string openDate;
		private string closeDate;
		private string stock;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Is buying stocks hitting new 52-week highs a profitable trading strategy? | Beyond The Charts Episode 002";
				Name										= "Ep002";
				// Begin initialization of our custom variables 
				startingBalance								= 10000;
				currentBalance								= 10000;
				realizedPnL									= 0.0;
				unrealizedPnL								= 0.0;
				openDate									= "";
				closeDate									= "";
				stock										= "";
				entryPrice									= 0.0;
				exitPrice									= 0.0;
				netTrade									= 0.0;
				rMultiple									= 0.0;
				stopDistance								= 10.0;
				// End initialization of our custom variables
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 2;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 252;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
			}
			else if (State == State.Configure)
			{}
		}
		// Primary strategy loop called on each update
		protected override void OnBarUpdate()
		{
			// Make sure we have enough data to compute indicators and run strategy logic.
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			CalculatePerformance();
			BuySellRules();
		}
		
		
		// Buy and sell rules
		private void BuySellRules()
		{
			// Determine if we have a new long entry
			if (ClosedAtFiftyTwoWeekHighs() && Positions[0].MarketPosition == MarketPosition.Flat)
			{
				EnterLong(ComputeShareSize(),"New 52 Week High");
				SetTrailStop("New 52 Week High", CalculationMode.Percent, (stopDistance/100), true);
			}
		}
		
		
		// Helper method to determine if today's close is setting new 52-week highs
		private Boolean ClosedAtFiftyTwoWeekHighs()
		{
			//Find the highest bar over the past 252 days
  			int highestBar = HighestBar(Close, 252);
  			//Store the highest price over the past 252 days
  			double fiftyTwoWeekHigh = Close[highestBar];  
			//Return true if today's close is the new 52-week high
			return (BarsArray[0].GetClose(CurrentBar) >= fiftyTwoWeekHigh);
		}		
		
		// Calculate the number of shares to purchase based on running account balance and current closing prices. Assumes 100% invested.
		private int ComputeShareSize()
		{
			return (int)Math.Floor(currentBalance/Close[0]);
		}
		
		// Helper method to track running Pnl and account balance.
		private void CalculatePerformance()
		{
			unrealizedPnL = (Position.GetUnrealizedProfitLoss(PerformanceUnit. Currency, Close[0]));
			realizedPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			currentBalance = startingBalance + unrealizedPnL + realizedPnL;
		}
		
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
		  // Log trade information once we get filled on our new highs signal
		  if (execution.Order.Name == "New 52 Week High" && execution.Order.OrderState == OrderState.Filled)
		  {
			    stock = execution.Order.Instrument.ToString().Substring(0,Instrument.ToString().IndexOf(" "));
			  	entryPrice = price;
				openDate = time.ToString("d");
		  }
  		  // Log trade information once our trailing stop gets triggered
		  if (execution.Order.Name == "Trail stop" && execution.Order.OrderState == OrderState.Filled)
		  {
			  	exitPrice = price;
			    netTrade =  Math.Round((exitPrice - entryPrice)/entryPrice*100,2);
			  	rMultiple = Math.Round(netTrade / stopDistance,2);
				closeDate = time.ToString("d");
			  	Print(stock+","+openDate+","+closeDate+","+entryPrice+","+exitPrice+","+netTrade+","+rMultiple);
		  }
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trail Stop Distance", Order=4, GroupName="Parameters")]
		public double stopDistance
			{ get; set; }
		#endregion
	}
}
