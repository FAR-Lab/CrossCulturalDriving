import socket
import sys
import os
import threading
import datetime
from queue import Queue

#today's date
date = str(datetime.date.today())

#create a socket for server 
server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

# retrieve local hostname
local_hostname = socket.gethostname()

# get the corresponding IP address
ip_address = socket.gethostbyname(local_hostname)

# get fully qualified hostname
local_fqdn = socket.getfqdn()

# arbitrary non-privileged port
port = 23456

#complete address required for binding
server_address = (ip_address, port)


# Declaration of variables to be used later in the program
# Change these as per your need
max_sim_num = 1 #maximum number of simulataneous simulation clients
path = "C:/Users/Public/Documents/FAR-Lab/Networking/Logs/" #folder path for log files
sep = ';' #Separator for the data values



#Directory Management
path = path + date +"/"
if (not os.path.exists(path)):
    opt = input("No folder exists for today. Want to create one? (y/n) ")
    if (opt.lower() == "y"):
        path += "Session_1/"
        os.makedirs(path)
    else:
        print ("Quitting this program")
        sys.exit()

else:
    m = 1
    if len(os.listdir(path)) != 0:
        m = max([int(i[-1]) for i in os.listdir(path)])
    
    path += "Session_"

    new = input("Want to create a new session? (y/n) ")
    if (new.lower() == "y"):
        m+=1
        path += str(m) + '/'
        os.makedirs(path)
    else :
        print ("Working in the most recent session which is Session_"+str(m))
        path += str(m) + '/'






# output hostname, domain name and IP address
print ("Working on %s (%s) with %s" % (local_hostname, local_fqdn, ip_address))

print ('Starting up on %s port %s' % server_address)
 
#Bind server socket to server address
try:
    server.bind(server_address)
except socket.error as msg:
    print ('Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1])
    sys.exit()
 
#Start listening on socket
server.listen(max_sim_num)
print ('Listening for client connections')

 
#Function for handling connections. This will be used to create threads
def client_handler(conn,addr):
    #Sending message to connected client
    
    #infinite loop so that function do not terminate and thread do not end.
    epoch = int((datetime.datetime.utcnow() - datetime.datetime.utcfromtimestamp(0)).total_seconds())
    p = path 
    p += addr[0] + "/"

    if (not os.path.exists(p)):
        os.makedirs(p)
    p += str(epoch) + "-"
    
    dat_buffer = Queue()
    isLogging = True
   

    def write():
        log_files = {}
        while isLogging:
            if not dat_buffer.empty():
                l = dat_buffer.get().split('\n')
                for i in l:
                    dat_id = i.split(sep)[0]
                    if dat_id != '':
                        if not (dat_id in log_files):
                            log_files[dat_id] = open(p + dat_id + ".txt" ,"a")

                        log_files[dat_id].write(i+'\n')

                for i in log_files:
                    log_files[i].flush()





    logging_thread = threading.Thread(target = write, args = ())
    logging_thread.start()

    while True:
         
        #Receiving from client
        data = conn.recv(2048)
        if not data: 
            isLogging = False
            print ('No data. Closing connection with '  + addr[0] + ':' + str(addr[1]))
            #log.close()
            conn.close()
            break
        
        else:
            print ("Data received")
            dat = data.decode("utf-8")
            #log.write(dat)
            dat_buffer.put(dat)     
        


 
#now keep talking with the client
while max_sim_num != 0:
    max_sim_num -= 1

    #wait to accept a connection - blocking call
    client, addr = server.accept()
    
    print ('Connected with ' + addr[0] + ':' + str(addr[1]))
    client_thread = threading.Thread(target = client_handler, args = (client,addr,))
    client_thread.start()
 
server.close()