#include <netdb.h> 
#include <netinet/in.h> 
#include <stdlib.h> 
#include <string.h> 
#include <sys/socket.h> 
#include <sys/types.h> 
#include <stdio.h>
#include <unistd.h>
#define MAX 14
#define PORT 8010 
#define SA struct sockaddr 
#include <wiringPi.h>
#include <softPwm.h>
#include <signal.h>
#include <stdlib.h>
#include <string.h>
#define LedPinRed    16
#define LedPinGreen  20
#define LedPinBlue   21
// Function designed for chat between client and server. 
#include <errno.h>
ssize_t
readn(int fd, void *vptr, size_t n){
	size_t nleft;
	ssize_t nread;
	char *ptr;

	ptr=vptr;
	nleft = n;
	while(nleft>0){
		if((nread=read(fd, ptr, nleft))<0){
			if(errno==EINTR){
				nread=0;
			}else
				return(-1);
		}else if(nread ==0) break;
		nleft-=nread;
		ptr +=nread;
	}//while
	return (n-nleft);
}//Reand


void func(int sockfd) 
{ 
	char buff[MAX]; 
	int n; 
	// infinite loop for chat 
	int r=255,g=255, b=255;
	int n1 = 0, n10=0, n100=0;	
	int lum=255;
	short encendido = 0;
	for (;;) { 
		bzero(buff, MAX); 

		// read the message from client and copy it in buffer 
		readn(sockfd, buff, sizeof(buff)); 
		// print buffer which contains the client contents 
		printf("From client: %s\t \n ", buff); 
		//bzero(buff, MAX); 
		/*
		n = 0; 
		// copy server message in the buffer 
		while ((buff[n++] = getchar()) != '\n') 
			; 

		// and send that buffer to client 
		write(sockfd, buff, sizeof(buff)); 
		*/
		// if msg contains "Exit" then server exit and chat ended. 
		/*if (strncmp("exit", buff, 4) == 0) { 
			printf("Saliendo del Socket...\n"); 
			break; 
		} */
		if(buff[0]=='E'){//Encendido
			encendido = 1;
			softPwmWrite(LedPinRed,   (int)(r*((float)lum)/255));  //change duty cycle
			softPwmWrite(LedPinGreen, (int)(g*((float)lum)/265));
			softPwmWrite(LedPinBlue,  (int)(b*((float)lum)/270));
		}
		else if(buff[0]== 'A'){//Apagado
			encendido = 0;
			softPwmWrite(LedPinRed,   0);  //change duty cycle
			softPwmWrite(LedPinGreen, 0);
			softPwmWrite(LedPinBlue,  0);
		}
		if(buff[0]=='C' && encendido==1){//Dato de color
			/*r_val = (color & 0xFF0000) >> 16;  //get red value
			g_val = (color & 0x00FF00) >> 8;   //get green value
			b_val = (color & 0x0000FF) >> 0;   //get blue value*/

			n1 = buff[4]-48;
			n10 = buff[3]-48;
			n100 = buff[2]-48;
			r = n100*100 + n10*10 + n1;
			n1 = buff[8]-48;
			n10 = buff[7]-48;
			n100 = buff[6]-48;
			g = n100*100 + n10*10 + n1;
			n1 = buff[12]-48;
			n10 = buff[11]-48;
			n100 = buff[10]-48;
			b = n100*100 + n10*10 + n1;
			printf("Información de color = #R%d  #G%d  #B%d \n\n", r, g, b);
			r = r*100/255 ;    //change a num(0~255) to 0~10
			g = g*100/255;
			b = b*100/255;
                       if(lum>=0){
				softPwmWrite(LedPinRed,   (int)(r*((float)lum)/255));  //change duty cycle
				softPwmWrite(LedPinGreen, (int)(g*((float)lum)/265));
				softPwmWrite(LedPinBlue,  (int)(b*((float)lum)/270));
			}
		}else	if(buff[0]=='L' && encendido==1){//Dato de luminosidad
			n1 = buff[3]-48;
			n10 = buff[2]-48;
			n100 = buff[1]-48;
			lum = n100*100 + n10*10 + n1;
			printf("Información de luminosidad = %d\n\n", lum);
			softPwmWrite(LedPinRed,   (int)(r*((float)lum)/255));  //change duty cycle
			softPwmWrite(LedPinGreen, (int)(g*((float)lum)/255));
			softPwmWrite(LedPinBlue,  (int)(b*((float)lum)/255));
		}
	} 
} 

int sockfd;

void manejador(int i){
	close(sockfd);
	exit(1); 
}

// Driver function 
int main() 
{ 
	wiringPiSetupGpio();
	int connfd, len; 
	struct sockaddr_in servaddr, cli; 
	printf("Iniciando Leds\n");
	softPwmCreate(LedPinRed,  0, 100);  //create a soft pwm, original duty cycle is 0Hz, range is 0~100
	softPwmCreate(LedPinGreen,0, 100);
	softPwmCreate(LedPinBlue, 0, 100);
	// socket create and verification 
	sockfd = socket(AF_INET, SOCK_STREAM, 0); 
	if (sockfd == -1) { 
		printf("Fallo creano el socket...\n"); 
		exit(0); 
	} 
	else
		printf("Socket creado correctamente..\n"); 
	bzero(&servaddr, sizeof(servaddr)); 

	signal(SIGINT,manejador);

	// assign IP, PORT 
	servaddr.sin_family = AF_INET; 
	servaddr.sin_addr.s_addr = htonl(INADDR_ANY); 
	servaddr.sin_port = htons(PORT); 

	// Binding newly created socket to given IP and verification 
	if ((bind(sockfd, (SA*)&servaddr, sizeof(servaddr))) != 0) { 
		printf("socket bind failed...\n"); 
		exit(0); 
	} 
	else
		printf("Socket successfully binded..\n"); 

	// Now server is ready to listen and verification 
	if ((listen(sockfd, 5)) != 0) { 
		printf("Listen failed...\n"); 
		exit(0); 
	} 
	else
		printf("Server listening..\n"); 
	len = sizeof(cli); 

	// Accept the data packet from client and verification 
	connfd = accept(sockfd, (SA*)&cli, &len); 
	if (connfd < 0) { 
		printf("server acccept failed...\n"); 
		exit(0); 
	} 
	else
		printf("server acccept the client...\n"); 

	// Function for chatting between client and server 
	func(connfd); 

	// After chatting close the socket 
	close(sockfd); 
} 

