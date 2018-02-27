/****************************************
 Filename: bankingapp.cs
 Author: Devin Hero
 Created: 2/21/2018
 Last Modified: 2/26/2018
****************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MemoryCache;

namespace HeroicLedgers
{
	class Program
	{
		static void Main(string[] args)
		{
			StartScreen();
		}
		
		/*** Interface screens ***/

		/* StartScreen()
		 *   Login screen that the user sees upon opening the porogram or logging out
		 */
		static void StartScreen()
		{
			string status = "Welcome to Heroic Ledgers!";
			ConsoleKeyInfo input;
			while (true)
			{
				ScreenRefresh(null, status);
				Console.WriteLine("Please choose a following option:");
				Console.WriteLine("(L) Login  (C) Create New Account  (Q) Quit");
				input = Console.ReadKey(true);

				switch (input.Key)
				{
					case ConsoleKey.L:
						status = Login();
						break;
					case ConsoleKey.C:
						status = CreateAccount();
						break;
					case ConsoleKey.Q:
						return;
					default:
						status = "Unknown command.";
						break;
				}
			}
		}

		/* UserConsole()
		 *   The user interface once the user has logged in, allowing the user to
		 *   interact with their account.
		 *   
		 *   Returns: Status string with logout information.
		 */
		static string UserConsole(string user, string pass)
		{
			ConsoleKeyInfo input;
			string statusMessage = "Welcome, " + user;

			while (true)
			{
				ScreenRefresh(user, statusMessage);
				Console.WriteLine("Please choose a following option:");
				Console.WriteLine("(W) Withdrawal  (D) Deposit  (B) Check Balance  (H) Transaction History  (Q) Logout");
				input = Console.ReadKey(true);

				switch (input.Key)
				{
					case ConsoleKey.W:
						statusMessage = Withdrawal(user, pass);
						break;
					case ConsoleKey.D:
						statusMessage = Deposit(user, pass);
						break;
					case ConsoleKey.B:
						statusMessage = CheckBalance(user, pass);
						break;
					case ConsoleKey.H:
						statusMessage = CheckTransactionHistory(user, pass);
						break;
					case ConsoleKey.Q:
						return "Logging out. Thank you for using Heroic Ledgers!";
					default:
						statusMessage = "Unknown command.";
						break;
				}
			}
		}

		/* ViewTransactions()
		 *   Allows the user to browse the pages of their transaction history
		 *   
		 *   Returns: Status string announcing end of history browsing
		 */
		static string ViewTransactions(string user, string[] history)
		{
			ConsoleKeyInfo input;
			int offset = 0, itemsPerScreen = 6;
			int offsetMax = Convert.ToInt32(Math.Floor( Convert.ToDouble(history.Length-1)/itemsPerScreen ));
			if (offsetMax < 0) offsetMax = 0; //Prevents "Page 1 of 0" listing if history is empty

			while (true)
			{
				ScreenRefresh(user, GetTransactHistOptions(offset, offsetMax));

				PrintTransactHistList(offset, offsetMax, itemsPerScreen, history);
				
				input = Console.ReadKey(true);
				switch (input.Key)
				{
					case ConsoleKey.LeftArrow:
						if(offset < offsetMax)
							offset++;
						break;
					case ConsoleKey.RightArrow:
						if (offset > 0)
							offset--;
						break;

					case ConsoleKey.Q:
						return "Done viewing transaction history.";
					default:
						break;
				}
			}
		}

		/*** User actions ***/

		/* CreateAccount()
		 *   Gets username, password, and password verification from user to create a 
		 *   new account.
		 *   
		 *   Returns: Status string with results of attempted account creation
		 */
		static string CreateAccount()
		{
			string pass, passVerify, username, status = "Creating new account.";
			while (true)
			{
				ScreenRefresh(null, status);
				Console.WriteLine("Please enter desired username (or type \"cancel\" to return):");
				username = Console.ReadLine();

				if(username == "")
				{
					status = "Error: Cannot create account with blank username.";
					continue;
				}
				if (username.ToLower() == "cancel" || username.ToLower() == "\"cancel\"")
					return "Account creation cancelled.";
				if (DBAct.DoesUserExist(username))
				{
					status = "Error: Account name \"" + username + "\" taken.";
					continue;
				}

				Console.WriteLine("Please enter password (will stay hidden):");
				pass = GetPasswordInput();
				Console.WriteLine("Please verify password:");
				passVerify = GetPasswordInput();

				if (passVerify == pass)
				{
					if (DBAct.CreateUser(username, pass))
						return "New account \"" + username + "\" created.";
					else
						return "Error creating account.";
				}
				else
					status = "Password mismatch. New account not created.";
			}
		}
		
		/* Login()
		 *   Gets username and password from user, then logs in (if authenticated)
		 *   
		 *   Returns: Status string with results of login attempt
		 */
		static string Login()
		{
			string user, pass;
			ScreenRefresh(null, "Logging in. Please provide username and password.");

			Console.WriteLine("Username:");
			user = Console.ReadLine();
			Console.WriteLine("Password:");
			pass = GetPasswordInput(); 

			if (DBAct.DoesPasswordMatch(user, pass))
				return UserConsole(user, pass);
			else
				return "Error: Could not authenticate credentials";
		}
		
		/* Withdrawal()
		 *   Gets amount from user, checks that money is available, confirms amount with
		 *   user, and withdraws the specified amount from the account.
		 *   
		 *   Returns: Status string with results of the attempted withdrawal
		 */
		static string Withdrawal(string user, string pass)
		{
			string amountInput;
			decimal amountNum, balance = 0;
			DBAct.GetBalance(user, pass, ref balance);

			ScreenRefresh(user, "Making withdrawal.");
			Console.WriteLine("Please enter amount to withdraw (ex: 3500.00 or 3500), or \"Q\" to return.");
			Console.WriteLine("Current balance: {0:0.00}", balance);
			amountInput = Console.ReadLine();

			if (amountInput.ToLower() == "q")
				return "Withdrawal cancelled.";
			if (!IsMoneyAmountFormatted(amountInput))
				return "Error: Incorrectly formatted amount.";
			amountNum = Convert.ToDecimal("-" + amountInput); //Withdrawals are negative when stored

			if (balance + amountNum < 0)
				return "Error: Insufficient funds.";

			ScreenRefresh(user, "Confirming withdrawal."); // Don't show the negative to the user, it's confusing
			Console.WriteLine("Confirm withdrawal amount of $" + (-amountNum).ToString("0.00") + "?");
			if (!ConfirmYesNo())
				return "Withdrawal cancelled.";

			if (DBAct.AddTransaction(user, pass, amountNum))
				return "$" + (-amountNum).ToString("0.00") + " withdrawn.";
			else
				return "Error: Withdrawal failed.";
		}

		/* Deposit()
		 *   Gets amount from user, confirms amount with user, and deposits the specified 
		 *   amount to the account.
		 * 
		 *   Returns: Status string with results of the attempted withdrawal
		 */
		static string Deposit(string user, string pass)
		{
			string amountInput;
			decimal amountNum;

			ScreenRefresh(user, "Making deposit.");
			Console.WriteLine("Please enter amount to deposit (ex: 3500.00 or 3500), or \"Q\" to return.");
			amountInput = Console.ReadLine();

			if (amountInput.ToLower() == "q")
				return "Deposit cancelled.";
			if (!IsMoneyAmountFormatted(amountInput))
				return "Error: Incorrectly formatted amount.";
			amountNum = Convert.ToDecimal(amountInput);

			ScreenRefresh(user, "Confirming deposit.");
			Console.WriteLine("Confirm deposit amount of $" + (amountNum).ToString("0.00") + "?");

			if (!ConfirmYesNo())
				return "Deposit cancelled.";

			if (DBAct.AddTransaction(user, pass, amountNum))
				return "$" + amountNum.ToString("0.00") + " deposited.";
			else
				return "Error: Deposit failed.";
		}

		/* CheckBalance()
		 *   Checks the account balance for the user.
		 *   
		 *   Returns: Status string with the user's balance, or an error
		 */
		static string CheckBalance(string user, string pass)
		{
			decimal balance = -999999999;

			if (!DBAct.GetBalance(user, pass, ref balance))
				return "Error: Could not retrieve balance.";

			return "Your balance is $" + balance.ToString("0.00") + ".";
		}

		/* CheckTransactionHistory()
		 *   Fetches account transaction records for user to view
		 *   
		 *   Returns: Status string with results of attempted view
		 */
		static string CheckTransactionHistory(string user, string pass)
		{
			string[] history = { };
			
			if (!DBAct.TransactionHistory(user, pass, ref history))
				return "Error: Could not retrieve history";

			return ViewTransactions(user, history);
		}

		/*** Helper functions, screen printing, etc ***/

		/*  GetPasswordInput()
		 *    Gets a censored password input from user
		 */
		static string GetPasswordInput()
		{
			string pass = null;
			while (true)
			{
				var key = Console.ReadKey(true);
				if (key.Key == ConsoleKey.Enter)
					break;
				if (key.Key == ConsoleKey.Backspace)
				{
					if(pass.Length > 0)
						pass = pass.Substring(0, pass.Length - 1);
					continue;
				}
				pass += key.KeyChar;
			}
			Console.WriteLine("");
			return pass;
		}

		/* ConfirmYesNo()
		 *   Prompts the user for a yes/no reply.
		 *   
		 *   Returns: true if yes, false if no
		 */
		static bool ConfirmYesNo()
		{
			Console.WriteLine("(Y) Yes  (N) No");
			ConsoleKeyInfo input = Console.ReadKey(true);
			while (!(input.Key == ConsoleKey.Y || input.Key == ConsoleKey.N))
				input = Console.ReadKey(true);

			if (input.Key == ConsoleKey.Y)
				return true;
			else
				return false;
		}

		/* IsMoneyAmountFormatted()
		 *   Checks that numeric input from user is transaction-friendly. Caps at
		 *   trillions place.
		 *   Examples:   3500   35.25   35.1   .35   .8
		 *   
		 *   Returns: true if well formatted, false if not
		 */
		static bool IsMoneyAmountFormatted(string amount)
		{
			Match match;
			Regex[] reg = { new Regex(@"^[0-9]{1,13}\.[0-9]{0,2}$"),
							new Regex(@"^[0-9]{1,13}$"),
							new Regex(@"^\.[0-9]{1,2}$")};

			for (int i = 0; i < reg.Length; i++)
			{
				match = reg[i].Match(amount);
				if (match.Success)
					return true;
			}

			return false;
		}
		
		/* GetTransactHistOptions()
		 *   Formats the buttons shown when viewing transaction history
		 *
		 *   Returns: Status string containing user instructions
		 */
		static string GetTransactHistOptions(int offset, int offsetMax)
		{
			string status = String.Format("Viewing transaction history. Page {0} of {1}\r\n", offset + 1, offsetMax + 1);

			if (offset < offsetMax)
				status += "Older (<-)";
			else
				status += "      (<-)";

			status += "      (Q) Quit      ";

			if (offset > 0)
				status += "(->) Newer";
			else
				status += "(->)";

			return status;
		}

		/* PrintTransactHistList()
		 *   Prints transaction items for the current transaction history page
		 */
		static void PrintTransactHistList(int offset, int offsetMax, int itemsPerScreen, string[] history)
		{
			int itemsToView;
			if (offset < offsetMax)
				itemsToView = itemsPerScreen;
			else
				itemsToView = history.Length - itemsPerScreen * offset;

			string[] list = new string[itemsToView];

			for (int i = 0; i < itemsToView; i++)
				list[i] = history[i + offset * itemsPerScreen];

			Array.Reverse(list); //We want newer transactions on bottom of the screen

			for (int i = 0; i < itemsToView; i++)
				Console.WriteLine(list[i]);
		}
		
		/* ScreenRefresh()
		 *   Resets screen with logo, current user (if logged in), and info on the
		 *   user's recent actions.
		 */
		static void ScreenRefresh(string user, string status)
		{
			Console.Clear();
			Console.WriteLine(
				" _____             _        __          _                 \r\n" +
				"|  |  |___ ___ ___|_|___   |  |   ___ _| |___ ___ ___ ___ \r\n" +
				"|     | -_|  _| . | |  _|  |  |__| -_| . | . | -_|  _|_ -|\r\n" +
				"|__|__|___|_| |___|_|___|  |_____|___|___|_  |___|_| |___|\r\n" +
				"                                         |___|            \r\n" +
				"                       Heroic Ledgers, (C) 2018 Devin Hero\r\n" +
				"__________________________________________________________"
				);
			if (user != null)
				Console.WriteLine("Logged in as {0}\r\n", user);
			Console.WriteLine(status + "\r\n");
		}
	}
	
	class DBAct
	{
		/*** Classes ***/
		
		public class UserRecords
		{
			private string name;
			public string Name
			{
				get { return name; }
				set { name = value; }
			}

			private string pass;
			public string Pass
			{
				get { return pass; }
				set { pass = value; }
			}

			private List<Transaction> records = new List<Transaction>();
			public List<Transaction> Records
			{
				get { return records; }
			}
			public void AddTransaction(Transaction trans)
			{
				records.Add(trans);
			}
			public decimal GetBalance()
			{
				decimal sum = 0;
				foreach(Transaction record in records)
				{
					sum += record.Amount;
				}
				return sum;
			}
		}
		
		public class Transaction
		{
			private DateTime date;
			public DateTime Date
			{
				get { return date; }
				set { date = value; }
			}

			private decimal amount;
			public decimal Amount
			{
				get { return amount; }
				set { amount = value; }
			}
		}

		/*** Data retrieval/manipulation. ***/

		/* CreateUser()
		 *   Creates a new user account, if the username is not already taken.
		 *   
		 *   Returns: true if account is created, false if not created
		 */
		public static bool CreateUser(string user, string pass)
		{
			string cacheName = GetCacheName(user);
			UserRecords newUserData = InitNewUserRecord(user, pass);

			if (Cache.Exists(cacheName))
				return false;
			
			Cache.Store(cacheName, newUserData);
			return true;
		}

		/* DoesUserExist()
		 *   Checks if username has an associated account created
		 *   
		 *   Returns: true if account exists, false if it doesn't
		 */
		public static bool DoesUserExist(string user)
		{
			string cacheName = GetCacheName(user);
			return Cache.Exists(cacheName);
		}

		/* DoesPasswordMatch()
		 *   Verifies credentials for an account.
		 *   
		 *   Returns: true if verified, false if password doesn't match or account does
		 *			  not exist
		 */
		public static bool DoesPasswordMatch(string user, string pass)
		{
			string cacheName = GetCacheName(user);
			if (Cache.Exists(cacheName))
			{
				DBAct.UserRecords userData = (DBAct.UserRecords)Cache.Get(cacheName);
				return (userData.Pass == pass);
			}
			return false;
		}

		/* AddTransaction()
		 *   Attempts to add a new transaction to the account. Withdrawals are passed as
		 *   negative amounts. Will fail if insufficient funds are detected.
		 *   
		 *   Returns: true if added, false if not added
		 */
		public static bool AddTransaction(string user, string pass, decimal amount)
		{
			string cacheName = GetCacheName(user);
			DBAct.UserRecords userData = null;

			if (!AuthUserAndFetchData(user, pass, ref userData))
				return false;

			if (userData.GetBalance() + amount < 0)
				return false;
			
			DateTime date = DateTime.Now;
			Transaction newTransaction = CreateNewTransaction(date, amount);
			userData.AddTransaction(newTransaction);
			Cache.Update(cacheName, userData);

			return true;
		}

		/* GetBalance()
		 *   Determines net balance of the user's account.
		 *   
		 *   Modifies: amount with the value of the user's balance
		 *   Returns: true if retrieved, false if not retrieved
		 */
		public static bool GetBalance(string user, string pass, ref decimal amount)
		{
			DBAct.UserRecords userData = null;

			if(!AuthUserAndFetchData(user, pass, ref userData))
				return false;
			
			amount = userData.GetBalance();
			return true;
		}

		/* TransactionHistory()
		 *   Determines the transaction history of the user.
		 *   
		 *   Modifies: history to store the account's transaction history. Newest items
		 *			   will begin at the start of the index.
		 *   Returns:  true if retrived, false if not retrieved
		 */
		public static bool TransactionHistory(string user, string pass, ref string[] history)
		{
			DBAct.UserRecords userData = null;

			if (!AuthUserAndFetchData(user, pass, ref userData))
				return false;

			history = new string[userData.Records.Count];

			for(int i = 0; i < userData.Records.Count; i++)
			{
				history[i] = Convert.ToString(userData.Records.ElementAt(i).Date) + " ... "
							+ String.Format("{0,17:0.00}", userData.Records.ElementAt(i).Amount);
			}
			Array.Reverse(history); //give newest items first

			return true;
		}

		/*** Helper functions, etc ***/

		/* AuthUserAndFetchData()
		 *   Checks that user exists, verifies credentials, and fetches account
		 *   infromation/records if account is authenticated
		 *   
		 *   Modifies: userData to hold the account data
		 *   Returns:  true or false reflecting if records could be retrieved
		 */
		public static bool AuthUserAndFetchData(string user, string pass, ref UserRecords userData)
		{
			string cacheName = GetCacheName(user);

			if (!DoesUserExist(user))
				return false;
			if (!DoesPasswordMatch(user, pass))
				return false;

			userData = (DBAct.UserRecords)Cache.Get(cacheName);

			return true;
		}
		
		/* InitNewUserRecord()
		 *   Initializes a new UserRecords object with user credentials
		 */
		public static UserRecords InitNewUserRecord(string user, string pass)
		{
			UserRecords newRecord = new UserRecords
			{
				Name = user,
				Pass = pass
			};
			return newRecord;
		}

		/* CreateNewTransaction()
		 *   Creates a new Transaction object 
		 */
		public static Transaction CreateNewTransaction(DateTime date, decimal amount)
		{
			Transaction newTransaction = new Transaction
			{
				Date = date,
				Amount = amount
			};
			return newTransaction;
		}

		/* GetCacheName()
		 *   Formats cache key for the specified user
		 */
		public static string GetCacheName(string user)
		{
			return "cache_" + user;
		}
	}
}
