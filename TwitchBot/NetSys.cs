//Version 0.0.3

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using BinaryFormatter = System.Runtime.Serialization.Formatters.Binary.BinaryFormatter;
using TwitchBot;

public class NetSys
{
    //Configure the PayloadLength values
    readonly Int16 MaxPayloadLength = 1480, PayloadDataLength = 8;
    //This is the server, it will handle all the clients and needs to be port forwarded
    public class Server
    {
        //configure the server
        NetSys netSys = new NetSys();
        TcpListener server;
        List<ClientConnection> clients = new List<ClientConnection>();
        bool isListening = true;
        //This is the main function, it will start the server and accept clients
        public async Task StartServer(int port = 12345)
        {
            // Start the server
            CreateServer(port);
            // Accept and handle client connections asynchronously
            await AcceptClientsAsync();
        }
        void CreateServer(int port)
        {
            // Create a TCP listener
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            netSys.Log($"Server started: {server.LocalEndpoint}");
        }
        async Task AcceptClientsAsync()
        {
            //while the server is listening, accept clients
            while (isListening)
            {
                // Accept a client connection
                TcpClient client = await server.AcceptTcpClientAsync();
                netSys.Log("Client connected: " + client.Client.RemoteEndPoint);
                //get a random id for the client that isn't already taken
                Random rnd = new Random();
                int id = rnd.Next(0, 9999);
                while(clients.Find(x => x.ID == id) != null){ id = rnd.Next(0, 9999); }
                // Handle the client connection asynchronously
                Task clientTask = HandleClientAsync(client, id);
                //add the client to the list of clients
                ClientConnection c = new ClientConnection();
                c.ClientTask = clientTask; c.ID = id; c.stream = client.GetStream();
                clients.Add(c);
                if (!isListening){ break; } //if the server is no longer listening, break the loop
            }
        }

        async Task HandleClientAsync(TcpClient client, int ID)
        {
            //create the buffers
            byte[] fBuffer = null, buffer, lenBuffer, bBuffer;
            int totalBytes = 0;
            try{
                using (NetworkStream stream = client.GetStream()){
                    // Handle client requests
                    while (client.Connected){
                        lenBuffer = new byte[4]; //create the length buffer
                        await stream.ReadAsync(lenBuffer, 0, lenBuffer.Length);
                        totalBytes = int.Parse(Encoding.ASCII.GetString(lenBuffer, 0, lenBuffer.Length));
                        totalBytes += netSys.PayloadDataLength; //add the payload data length
                        buffer = new byte[totalBytes]; bBuffer = new byte[totalBytes - 4];
                        await stream.ReadAsync(bBuffer, 0, bBuffer.Length);

                        //make buffer = lenBuffer + bBuffer
                        for (int i = 0; i < lenBuffer.Length; i++) { buffer[i] = lenBuffer[i]; }
                        for (int i = 0; i < bBuffer.Length; i++) { buffer[i + 4] = bBuffer[i]; }

                        if (totalBytes == 0) { break; } //might be redundant
                        netSys.Log("Recived Data From Client");
                        fBuffer = netSys.Recive(buffer, true, ID, fBuffer); //get just the data from the buffer
                        //if we are expecting a response, confirm the data was recieved
                        if(fBuffer != null) { await netSys.DataOK(stream); } 
                    }
                }
            }
            catch (Exception ex)
            { netSys.Log("Client Lost: " + ex.Message); }
            // Close the client connection
            client.Close();
            netSys.Log("Client disconnected: " + client.Client.RemoteEndPoint);
        }
        //stop listening for clients
        public void CloseServer()
        { isListening = false; }

        public async Task RespondToClient(object data, int payloadID, int clientID){
            //find the client with the id
            ClientConnection c = clients.Find(x => x.ID == clientID);
            if(c == null){ netSys.Log($"Client with id {clientID} not found"); return; }
            //send the data to the client
            await netSys.Send(data, payloadID, c.stream);
        }

        class ClientConnection{
            public Task ClientTask;
            public int ID;
            public NetworkStream stream;
        }
    }
    public class Client
    {
        //configure the client
        NetSys netSys = new NetSys();
        TcpClient server;
        NetworkStream stream;
        bool isBusy = false;

        public bool Connect(string ipAddress, int port = 12345)
        {
            // Connect to the receiver
            server = new TcpClient();
            try{
                server.Connect(ipAddress, port);
            }
            catch(Exception ex){
                netSys.Log($"Failed to connect to server: {ex.Message}");
                return false;
            }

            netSys.Log("Connected to the server!");

            // Get the network stream for sending and receiving data
            stream = server.GetStream();
            return true;
        }
        public async Task SendData(object data, int payloadId, bool listenForResponse = false){
            while(isBusy){ await Task.Delay(7); 
            netSys.Log("Waiting for previous data to send"); }
            isBusy = true;
            netSys.Log("Sending Data To Server");
            await netSys.Send(data, payloadId, stream);
            netSys.Log("Data Sent");
            if(!listenForResponse){
                isBusy = false;
                return;
            }
            isBusy = true;
            await GetResponse();
        }
        async Task GetResponse()
        {
            byte[] fBuffer = null, buffer, lenBuffer, bBuffer;
            // Set the idle timeout to 5 seconds
            TimeSpan idleTimeout = TimeSpan.FromSeconds(5);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(idleTimeout);
            try
            {
                // Handle client requests
                while (server.Connected)
                {
                    //Log("Data check?");
                    lenBuffer = new byte[4];
                    int bytesRead = await stream.ReadAsync(lenBuffer, 0, lenBuffer.Length, cancellationTokenSource.Token);
                    int totalBytes = int.Parse(Encoding.ASCII.GetString(lenBuffer, 0, bytesRead));
                    totalBytes += 8;
                    //Log($"Totalbytes: {totalBytes}");
                    buffer = new byte[totalBytes];
                    bBuffer = new byte[totalBytes - 4];
                    bytesRead = await stream.ReadAsync(bBuffer, 0, bBuffer.Length, cancellationTokenSource.Token);

                    //make buffer = lenBuffer + bBuffer
                    for (int i = 0; i < lenBuffer.Length; i++) { buffer[i] = lenBuffer[i]; }
                    for (int i = 0; i < bBuffer.Length; i++) { buffer[i + 4] = bBuffer[i]; }

                    if (bytesRead == 0)
                        break;

                    fBuffer = netSys.Recive(buffer, false, 0, fBuffer);
                    if (fBuffer != null) { await netSys.DataOK(stream); }
                    else { isBusy = false; netSys.Log("Recived Data From Server"); return; }
                    
                    // Reset the timeout if data was received
                    cancellationTokenSource.CancelAfter(idleTimeout);
                }
            }
            catch (OperationCanceledException)
            {
                // The operation was canceled due to idle timeout
                netSys.Log("Idle timeout occurred.");
                isBusy = false;
            }
            catch (Exception ex)
            {
                netSys.Log("Server Lost: " + ex.Message);
            }
            
            // Close the client connection
            server.Close();
            netSys.Log("Server disconnected: " + server.Client.RemoteEndPoint);
        }
        
        public async Task Disconnect()
        {
            // Clean up
            stream.Close(); server.Close();
            netSys.Log("Disconnected from the server!");
        }
    }
    // =================================================================================================
    // THIS IS THE MAIN FUNCTION EDIT THIS =============================================================
    // =================================================================================================
    void ProcessData(byte[] data, int payloadId, bool isServer, int ID)
    {
        //ID is the client id if isServer is true
        object dataObject = ByteArrayToObject(data);
        //if id is 1, then it's a string
        if (payloadId == 1)
        {
            string dataString = (string)dataObject;
            Log($"Received string: {dataString}");
        }
        else if(payloadId == 2 && isServer)
        {
            //this is a client request, it will contain a string path to the bot client
            //Program.GetDataForClient(ID, (string)dataObject);
        }
        else if(payloadId == 3){
            //this is a client response, it will contain a encrypted save file for the bot client
            Program.decodeString = (string)dataObject;
            //Program.Log("Recived Data From Server");
        }
    }
    // =================================================================================================
    // =================================================================================================
    // =================================================================================================

    async Task Send(object data, int payloadId, NetworkStream stream)
    {
        
        // Convert the object to bytes
        byte[] dataBytes = ObjectToByteArray(data);
        //Log($"Sending {dataBytes.Length} bytes of data");

        // Split the data into multiple payloads if necessary
        List<byte[]> payloads = SplitDataIntoPayloads(dataBytes);

        // Send each payload
        for (int i = 0; i < payloads.Count; i++)
        {
            byte[] payload = payloads[i];

            // Determine if this is the last payload
            bool isLastPayload = i == payloads.Count - 1;
            // Determine the terminator value
            char terminator = isLastPayload ? '1' : '2';
            // Determine the length of the payload
            int payloadLength = payload.Length + PayloadDataLength; // lengthBytes + payloadId + payloadData + terminator
            string info = $"{payload.Length.ToString("0000")}{payloadId.ToString("000")}{terminator}";
            byte[] infoBytes = Encoding.ASCII.GetBytes(info);
            // Create the complete payload
            byte[] completePayload = new byte[payloadLength];
            int index = 0;

            Array.Copy(infoBytes, 0, completePayload, index, infoBytes.Length);
            index += infoBytes.Length;
            //Log($"payloadLength: {payloadLength}, info: {info}");
            Array.Copy(payload, 0, completePayload, index, payload.Length);
            index += payload.Length;
            // Send the payload
            await SendPayloadAsync(completePayload, stream);
            Log($"Payload {i + 1}/{payloads.Count} sent.");
            // Wait for response if it's not the last payload
            if (!isLastPayload) {Log("Waiting For Recived"); await WaitForResponseAsync(stream); }
        }
    }
    byte[] Recive(byte[] toProcess,bool isServer, int ID, byte[] previous = null){
        //if previous is null, then this is the first payload
        // get the first 8 bytes as a string
        string info = Encoding.ASCII.GetString(toProcess, 0, 8);
        //id is bytes 4-6
        int payloadId = int.Parse(info.Substring(4, 3));
        //terminator is byte 7
        int terminator = int.Parse(info.Substring(7, 1));
        //Log($"PayloadId: {payloadId}, Terminator: {terminator}, from info: {info}");
        //remove the first 8 bytes to get the payload
        byte[] payload = new byte[toProcess.Length - 8];
        Array.Copy(toProcess, 8, payload, 0, toProcess.Length - 8);

        if(previous != null){
            byte[] combined = new byte[previous.Length + payload.Length];
            Array.Copy(previous, 0, combined, 0, previous.Length);
            Array.Copy(payload, 0, combined, previous.Length, payload.Length);
            payload = combined;
        }
        
        if (terminator == 1){
            //this is the last payload
            //process the data
            ProcessData(payload, payloadId, isServer, ID);
            return null;
        }
        else if (terminator == 2){ return payload; } //this is not the last payload
        else{
            //this is not a valid payload
            Log("Invalid payload"); return null;   
        }
    }
    async Task WaitForResponseAsync(NetworkStream stream)
    {
        byte[] responseBuffer = new byte[2];
        string response = "";
        //Log("Waiting for response...");
        while (!response.Contains("ok"))
        {
            int bytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            response += Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
        }
        //Log("Response: " + response);
    }
    async Task SendPayloadAsync(byte[] payload, NetworkStream stream)
    {
        try{ await stream.WriteAsync(payload, 0, payload.Length); }
        catch{ Log("Error sending payload"); }
        
    }
    List<byte[]> SplitDataIntoPayloads(byte[] data)
    {
        List<byte[]> payloads = new List<byte[]>();

        int remainingLength = data.Length;
        int startIndex = 0;

        while (remainingLength > 0)
        {
            int currentPayloadLength = Math.Min(remainingLength, MaxPayloadLength - PayloadDataLength);
            byte[] payload = new byte[currentPayloadLength];
            Array.Copy(data, startIndex, payload, 0, currentPayloadLength);
            payloads.Add(payload);

            remainingLength -= currentPayloadLength;
            startIndex += currentPayloadLength;
        }

        return payloads;
    }
    byte[] ObjectToByteArray(Object b){
        if(b == null)
            return null;
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            bf.Serialize(ms, b);
            return ms.ToArray();
        }
    }
    //Byte array to object
    Object ByteArrayToObject(byte[] arrBytes){
        using (MemoryStream memStream = new MemoryStream())
        {
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }
    }
    async Task DataOK(NetworkStream stream)
    {
        byte[] buffer = Encoding.ASCII.GetBytes("ok");
        //Log($"ok length: {buffer.Length}");
        await stream.WriteAsync(buffer, 0, buffer.Length);
    }
    void Log(string message)
    {
        //Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {message}");
    }
}