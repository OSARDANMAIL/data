using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design;
using System.Web;
using static Google.Protobuf.Reflection.SourceCodeInfo.Types;
using static Org.BouncyCastle.Bcpg.Attr.ImageAttrib;

public class Mainclass
{
    // Declare a static Boolean variable 'debug' and set it to true.
    static Boolean debug = true;


    // Declare a static string variable 'connStr' and initialize it with a MySQL connection string.
    static string connStr = "server=localhost; user=root; database=users ;port=3306;password=L3tM31n";

    // The entry point of the program.
    public static void Main(string[] args)
    {

        // Check if command line arguments were provided
        if (args.Length == 0)
        {
            // If no arguments provided, print a message indicating the server is starting.
            Console.WriteLine("Starting Server");

            // Call the 'RunServer' method.
            RunServer();
        }
        else
        {

            // If command line arguments were provided, create a MySqlConnection object 'conn'.
            using (MySqlConnection conn = new MySqlConnection(connStr))
            {
                try
                {
                    // Print a message indicating an attempt to connect to the MySQL database.
                    Console.WriteLine("Connecting to MySQL--- world database");

                    // Open the database connection.
                    conn.Open();

                }
                catch (Exception ex)
                {
                    // If an exception is caught, print the exception details.
                    Console.WriteLine(ex.ToString());
                }
                //conn.Close(); // close the connection
                Console.WriteLine("Done the job");

                // Iterate through the command line arguments and process each one using the 'ProcessCommand' method.
                for (int i = 0; i < args.Length; i++)
                {
                    ProcessCommand(conn, args[i]);
                }
            }

        }
    }

    // Define a method to handle HTTP requests.
    static void doRequest(NetworkStream socketStream, MySqlConnection connection)
    {

        // Initialize a StreamWriter and StreamReader to send and receive data over the network.
        StreamWriter sw = new StreamWriter(socketStream);
        StreamReader sr = new StreamReader(socketStream);

        // Optional debugging information
        if (debug) Console.WriteLine("Waiting for input from client...");

        // Read the request line from the client.
        String line = sr.ReadLine();
        Console.WriteLine($"Received Network Command: '{line}'");

        // Check if the request is a POST request to the root directory.
        if (line == "POST / HTTP/1.1")
        {

            // Debug information for POST request
            if (debug) Console.WriteLine("Received an update request");

        }

        // Check if the request is a GET request with a query parameter 'name'.
        else if (line.StartsWith("GET /?name=") && line.EndsWith(" HTTP/1.1"))
        {

            // Debug information for GET request.
            if (debug) Console.WriteLine("Received a lookup request");

            // Split the request line into components
            String[] slices = line.Split(" ");  // Split into 3 pieces

            // Extract the 'name' value from the URL.
            String ID = slices[1].Substring(7);

            // Process the GET request and get the result.
            String result = ProcessGetRequest(connection, ID);


            // If a result is found, send a 200 OK response with the result
            if (!string.IsNullOrEmpty(result))
            {
                sw.WriteLine("HTTP/1.1 200 OK");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.WriteLine(result);
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '{result}'");
            }
            else
            {
                // If no result is found, send a 404 Not Found response.
                sw.WriteLine("HTTP/1.1 404 Not Found");
                sw.WriteLine("Content-Type: text/plain");
                sw.WriteLine();
                sw.Flush();
                Console.WriteLine($"Performed Lookup on '{ID}' returning '404 Not Found'");
            }
        }
        else
        {
            // Handle unrecognized commands with a 400 Bad Request response.
            Console.WriteLine($"Unrecognised command: '{line}'");
            sw.WriteLine("HTTP/1.1 400 Bad Request");
            sw.WriteLine("Content-Type: text/plain");
            sw.WriteLine();
            sw.Flush();
        }
    }


    // Define a method to process a GET request to retrieve a user's location from the database.
    static String ProcessGetRequest(MySqlConnection connection, String username)
    {
        try
        {

            // SQL query to select the user's location.
            // It joins the 'user_login' and 'users_location' tables
            // and filters the result where the 'LoginID' matches the provided username.
            String query = @"
          SELECT uloc.Location
          FROM user_login ul
          INNER JOIN users_location uloc ON ul.UserID = uloc.UserID
          WHERE ul.LoginID = @username;
      ";

            // Create a MySQL command object with the query and connection.
            using (MySqlCommand cmd = new MySqlCommand(query, connection))
            {

                // Replace the '@username' parameter in the SQL query with the provided username.
                cmd.Parameters.AddWithValue("@username", username);

                // Execute the query and store the result.
                object result = cmd.ExecuteScalar();


                // Check if the result is not null or a DBNull (database null).
                // If so, return the location as a string.
                if (result != null && result != DBNull.Value)
                {
                    return result.ToString();
                }
                else
                {
                    // Return a message indicating that the location was not found for the username.
                    return "Location not found for username: " + username;
                }
            }
        }
        catch (Exception ex)
        {
            // Catch any exceptions that occur during the database operation.
            // Log the exception message to the console.
            Console.WriteLine($"Error in ProcessGetRequest: {ex.Message}");

            // Return a generic error message.
            return "Error retrieving location.";
        }
    }


    // Define a method to run a TCP server.
    static void RunServer()
    {
        TcpListener listener;
        Socket connection;
        NetworkStream socketStream;

        // Create a MySqlConnection using the connection string 'connStr'.
        // The 'using' statement ensures that the connection is closed and disposed properly.
        using (MySqlConnection conn = new MySqlConnection(connStr))
        {

            try
            {

                conn.Open();

                // Initialize a TcpListener to listen on port 43.
                listener = new TcpListener(43);

                // Start the listener to accept incoming connections.
                listener.Start();

                bool isRunning = true;

                // Loop indefinitely to handle incoming connections.

                while (true)
                {

                    // If debug mode is enabled, print a waiting message.
                    if (debug) Console.WriteLine("Server Waiting connection...");

                    // Accept an incoming socket connection.
                    connection = listener.AcceptSocket();

                    // Set send and receive timeouts for the socket.
                    connection.SendTimeout = 1000;
                    connection.ReceiveTimeout = 1000;

                    // Create a NetworkStream to read and write data over the socket.
                    socketStream = new NetworkStream(connection);

                    // Process the request using the 'doRequest' method
                    doRequest(socketStream, conn);

                    // Close the network stream and the socket connection.
                    socketStream.Close();
                    connection.Close();
                }
            }

            catch (Exception ex)
            {
                // Catch any exceptions that occur during server operation.
                // Log the exception message to the console.
                Console.WriteLine(ex.ToString());
            }
            if (debug)
                Console.WriteLine("Terminating Server");

        }


    }
    /// Process the next database command request
    static void ProcessCommand(MySqlConnection connection, string command)
    {

        // Prints the received command to the console.
        Console.WriteLine($"\nCommand: {command}");

        // Wraps the code in a try-catch block to handle any exceptions.
        try
        {
            // Splits the command string into two parts at the first occurrence of '?'.
            String[] slice = command.Split(new char[] { '?' }, 2);


            // Assigns the first part of the command to 'ID'.
            String ID = slice[0];
            // Declares variables for operation, update, and field.
            String operation = null;
            String update = null;
            String field = null;

            // Checks if the command was split into two parts.
            if (slice.Length == 2)
            {
                // Assigns the second part to 'operation'.
                operation = slice[1];

                // Splits 'operation' into two parts at the first occurrence of '='.
                String[] pieces = operation.Split(new char[] { '=' }, 2);

                // Assigns the first part to 'field'.
                field = pieces[0];

                // Checks if the operation was split into two parts.
                if (pieces.Length == 2) update = pieces[1];
            }


            // If no operation is specified, calls the Dump function.
            if (operation == null) Dump(connection, ID);
            // If no update value is specified, calls the Lookup function.
            else if (update == null) Lookup(connection, ID, field);

            // If both field and update values are present, calls the Update function.
            else Update(connection, ID, field, update);

            // Re-checks if the command was split into two parts.
            if (slice.Length == 2)
            {
                // Reassigns the second part to 'operation'.
                operation = slice[1];
                if (string.IsNullOrEmpty(operation.Trim('?')))
                {
                    Delete(connection, ID);
                    return;
                }

            }
        }
        // Catches any exceptions that occur during the process.
        catch (Exception ex)
        {
            // Logs the exception details to the console.
            Console.WriteLine($"Fault in Command Processing: {ex.ToString()}");


        }


    }


    // Declares the Delete method, which takes a MySqlConnection and an ID string as parameters.
    static void Delete(MySqlConnection connection, String ID)
    {
        // If debug mode is on, logs the delete action to the console.
        if (debug) Console.WriteLine($"Delete record '{ID}' from DataBase");

        // Formats the SQL command to delete a record with the given ID from 'user_login' table.
        string sqlcommand = $@"SET SQL_SAFE_UPDATES = 0;

          DELETE FROM user_login WHERE loginID = ""{ID}"" ;";


        // Executes the SQL command using the connection.
        DoQuery(connection, sqlcommand);
    }

    // Declares the Dump method for displaying user data based on an ID.
    static void Dump(MySqlConnection connection, String ID)
    {
        // Checks if the ID exists in the database, if not, prints a message and exits the method.
        if (!ifIDEXIST(connection, ID))
        {

            Console.WriteLine($"User '{ID}' can't be found");
            return;
        }


        // If debug mode is on, logs a message indicating outputting all fields.
        if (debug) Console.WriteLine(" output all fields");

        // Formats an SQL query to select various user details from different tables joined by UserID.
        String sqlcommand = $@"  SELECT
    up.UserID,
    up.Surname,
    up.Forenames,
    up.Title,
    up.Position,
    uph.Phone,
    ed.Email,
    uloc.Location
FROM
    emaildetails ed
JOIN user_login ul ON ed.UserID = ul.UserID
JOIN user_names un ON ed.UserID = un.UserID
JOIN userinfo up ON ed.UserID = up.UserID
JOIN user_phone uph ON ed.UserID = uph.UserID
JOIN users_location uloc ON ed.UserID = uloc.UserID
WHERE
    ul.LoginID = '{ID}';
";

        // If debug mode is on, logs the SQL query.

        if (debug)
        {
            Console.WriteLine($"Sql Query for all field: {sqlcommand}");
        }

        // Executes the SQL query using the connection.
        DoQuery(connection, sqlcommand);

    }

    // Declares the Lookup method for retrieving specific information based on an ID and a field.
    static void Lookup(MySqlConnection connection, string ID, string field)
    {
        // Checks if the field is null or whitespace, if so, deletes the record and exits the method.
        if (string.IsNullOrWhiteSpace(field))
        {
            Console.WriteLine($"Delete record '{ID}' from database");
            Delete(connection, ID);
            return;
        }

        // Formats an SQL query to select a specific field (location) from joined tables based on LoginID.
        string sqlcommand = $@"SELECT uloc.Location
                            FROM user_login ul
                            JOIN users_location uloc ON ul.UserID = uloc.UserID
                            WHERE ul.LoginID = '{ID}'";

        // If debug mode is on, logs the SQL query being executed.
        if (debug) { Console.WriteLine($"Executing SQL Query: {sqlcommand}"); }

        // Executes a specialized query for lookup using the connection, SQL command, and field.
        DoQuery4lookup(connection, sqlcommand, field);
    }

    // Defines the Update method for modifying database records.
    static void Update(MySqlConnection connection, String ID, String field, String update)
    {
        // Logs the field to be updated and the new value if debugging is enabled.
        if (debug) Console.WriteLine($" update field '{field}' to '{update}'");

        // Checks if the ID exists in the database; if not, inserts new details
        if (!ifIDEXIST(connection, ID))
        {
            insertnewdetails(connection, ID);
        }

        // Formats an SQL command to update a specific field(Location) for a given UserID.
        string sqlcommand = $@"UPDATE users_location
                            SET Location = '{update}'
                            WHERE UserID IN (SELECT UserID FROM user_login WHERE LoginID = '{ID}');";

        // Executes the update query using the provided connection.
        DoUpdate(connection, sqlcommand);

        Console.WriteLine("OK");
    }

    // Declares a method to check if a given ID exists in the database.
    static bool ifIDEXIST(MySqlConnection connection, string ID)
    {
        // Prepares an SQL command to count entries with the given ID.
        string sqlcommand = $@"SELECT COUNT(*) FROM user_login WHERE LoginID = '{ID}';";

        // Creates a new MySqlCommand object and executes it.
        using (MySqlCommand cmd = new MySqlCommand(sqlcommand, connection))
        {

            // Executes the command and converts the result to an integer.
            int countnumberofid = Convert.ToInt32(cmd.ExecuteScalar());


            // Returns true if the count is greater than 0, indicating the ID exists.
            return countnumberofid > 0;

        }
    }

    // Defines the DoQuery method, taking a MySqlConnection and a SQL command string as parameters.
    static void DoQuery(MySqlConnection connection, String sqlcommand)
    {
        // Using block to ensure proper disposal of the MySqlCommand object.
        using (MySqlCommand cmd = new MySqlCommand(sqlcommand, connection))
        {
            // Executes the SQL command and retrieves a reader for the result.
            using (MySqlDataReader rdr = cmd.ExecuteReader())
            {
                // Loops through each row in the result set.
                while (rdr.Read())
                {

                    // Iterates over each field in the current row.
                    for (int i = 0; i < rdr.FieldCount; i++)
                    {
                        // Prints each field's name and value to the console.
                        Console.WriteLine($"{rdr.GetName(i)} ={rdr[i]}");
                    }

                }
                // Checks if the result set is empty (no rows found).
                if (!rdr.HasRows)
                {


                    Console.WriteLine("result delected successfully");
                    return;
                }

            }


        }

    }

    // Defines the DoQuery4lookup method to execute a given SQL command for lookup purposes.
    static void DoQuery4lookup(MySqlConnection connection, String sqlcommand, string field)
    {
        // Wraps the code in a try-catch block for exception handling.
        try
        {

            // Creates a new MySqlCommand using the provided SQL command and connection.
            using (MySqlCommand cmd = new MySqlCommand(sqlcommand, connection))
            {

                // Executes the command and retrieves a MySqlDataReader to read the results.
                using (MySqlDataReader rdr = cmd.ExecuteReader())
                {

                    // Checks if the result set is empty.
                    if (!rdr.HasRows)
                    {
                        Console.WriteLine("Result not Found. ");
                        return;


                    }


                    // Iterates through each row in the result set.
                    while (rdr.Read())
                    {
                        int indexcolumn;
                        // Tries to get the column index of the specified field.
                        try
                        {
                            indexcolumn = rdr.GetOrdinal(field);
                        }
                        catch (IndexOutOfRangeException)
                        {
                            Console.WriteLine($"Field '{field}' can't be found in the result set. ");
                            return;
                        }
                        // Check if the specified field exists in the result set
                        if (rdr[indexcolumn] != DBNull.Value)
                        {
                            Console.WriteLine(rdr[field]);
                        }
                        else
                        {
                            Console.WriteLine($"Field '{field}' is DBNull in the result set.");
                        }
                    }


                }
            }
        }
        catch (Exception ex)
        {


            Console.WriteLine($"Error in Doqueryforlookup: {ex.ToString()}");

        }




    }

    // Defines the DoUpdate method to execute an update SQL command.
    static void DoUpdate(MySqlConnection connection, String sqlcommand)
    {
        // Logs the SQL command to be executed to the console.
        Console.WriteLine($"Executing update query: {sqlcommand}");


        // Using block to ensure proper disposal of the MySqlCommand object.
        using (MySqlCommand cmd = new MySqlCommand(sqlcommand, connection))
        {
            // Executes the SQL command and stores the number of rows affected.
            int rowsAffected = cmd.ExecuteNonQuery();

            // Checks if any rows were affected by the update.
            if (rowsAffected > 0)
            {

                // Logs a success message along with the count of affected rows.
                Console.WriteLine($"Update successful. {rowsAffected} row(s) affected.");
            }
            else
            {
                // Logs a message if the update did not affect any rows.
                Console.WriteLine("Update did not affect any rows.");
            }
        }
    }

    // Defines the insertnewdetails method to add new records to the database.
    static void insertnewdetails(MySqlConnection connection, string ID)
    {

        // Wraps the main logic in a try-catch block to handle exceptions.
        try
        {

            String sqlQuery = $@"INSERT INTO user_login (LoginID, UserID) VALUES ('{ID}', '{ID}');";

            String sqlQuery2 = $@"INSERT INTO users_location (Location, UserID) VALUES ('', '{ID}');";


            // Starts a transaction to ensure that both insertions are processed as a single atomic operation.
            using (MySqlTransaction transaction = connection.BeginTransaction())
            {
                try
                {
                    // Executes the first SQL query within the transaction
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Executes the second SQL query within the same transaction.
                    using (MySqlCommand cmd = new MySqlCommand(sqlQuery2, connection, transaction))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Commits the transaction to finalize the changes.
                    transaction.Commit();


                }
                catch (Exception ex)
                {
                    // Rolls back the transaction in case of an error.
                    transaction.Rollback();

                    // Logs the error message to the console.
                    Console.WriteLine($"Error in insertnewdetails: {ex.ToString()}");

                    Console.WriteLine(sqlQuery.ToString());
                    Console.WriteLine(sqlQuery2.ToString());
                }


            }



        }
        catch (Exception ex) // Catches any exceptions that might occur outside the transaction logic.
        {
            Console.WriteLine($"error in insertnewdetails: {ex.ToString()}");
        }







    }

}
