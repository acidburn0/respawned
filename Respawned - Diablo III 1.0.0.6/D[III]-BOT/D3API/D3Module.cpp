#include "D3Module.h"

//Uncomment the define below to build for XP as well as select Staff_ReleaseXP
//#define XP_RELEASE

char hexToAscii(char first, char second) 
{ 
    char hex[5], *stop; 
    sprintf(hex,"0x%c%c",first,second); 
    return strtol(hex, &stop, 16); 
}

DWORD DisableWarden()
{
	byte DisableWarden[] = {0xFF, 0xD3, 0x8B, 0xCE, 0x84, 0xC0}; 
	while(!GetModuleHandleA( "battle.net.dll" )) Sleep(5);
	DWORD dwOldProtection = NULL;
	DWORD WardenBase = ( DWORD )GetModuleHandleA( "battle.net.dll" );

	PIMAGE_NT_HEADERS pNTHeaders = ( PIMAGE_NT_HEADERS )( WardenBase + ((PIMAGE_DOS_HEADER )WardenBase)->e_lfanew );
	WardenBase += pNTHeaders->OptionalHeader.BaseOfCode;

	byte* startScan = (byte*)WardenBase,*endScan = (byte*)((DWORD)( WardenBase + (DWORD)pNTHeaders->OptionalHeader.SizeOfCode));

	while(startScan!=endScan)
	{
		if(!memcmp(startScan,DisableWarden,sizeof(DisableWarden)))
		{
			VirtualProtect(startScan+0x6, 2, PAGE_EXECUTE_READWRITE, &dwOldProtection);
			memcpy(startScan+0x6,"\xEB",1);
			VirtualProtect(startScan+0x6, 2, dwOldProtection, &dwOldProtection);
			return (DWORD)(startScan+0x6);
		}
		++startScan;
	}
	MessageBox(NULL, "Couldn't disable warden :(", NULL, MB_OK);
	return TerminateProcess((HANDLE)0xFFFFFFFF, 0);
}

int inArray(int* arr, int size, int value){
	for(int i=0; i< size; ++i){
		if(arr[i] == value) return 1;
	}
	return 0;
}

bool checkAuthCodeSection(int pCRC){
unsigned char buf[85];
int codes[] = {0x83,0x50,0xe8,0x89,0x0f,0xba,0xc1,0x8b,0x74,0x7e,0xeb,0xc7,0x8a};
int total_value = 0;
int mCRC = 0;
#ifdef XP_RELEASE
byte *checkat = (byte*)pValidCode+0x3BF;
#else
byte *checkat = (byte*)pValidCode+0x40A;
//printf("%08X\n",checkat);
#endif
memcpy(buf,checkat,(sizeof(buf)/sizeof(unsigned char))-1);
for(int o = 0;o<(sizeof(buf)/sizeof(unsigned char))-1; ++o){
	//printf("%02X\n",buf[o]);
}
for(int i=0; i<(sizeof(buf)/sizeof(unsigned char)); ++i){
		//printf("looking for:%02X\n",(int)buf[i]);
	if(inArray(codes,(sizeof(codes)/sizeof(int)),(int)buf[i])) total_value += (int)buf[i];
}
mCRC = (total_value/(sizeof(buf)/((sizeof(unsigned char)+1))+1)+total_value);
if(mCRC == pCRC) return true;
//char error[32] = {0};
//sprintf(error,"%d!=%d",mCRC,pCRC);
//MessageBoxA(0,error,"failed!",0);
return false;
}

void Reauth()
{
	typedef bool (*CheckCode)(char* code, char* version);
	HKEY hKey = 0;
	char buf[100] = {0};
	char auth_key[100] = {0};
	int gCRC = 0;
    DWORD dwType = 0;
#ifdef XP_RELEASE
	gCRC = 2802;
#else
	gCRC = 2679;
#endif
    DWORD dwBufSize = sizeof(buf);
    const char* subkey = "Software\\DIIIB";
		if( RegOpenKey(HKEY_CURRENT_USER,subkey,&hKey) == ERROR_SUCCESS)
		{
			dwType = REG_SZ;
			if(RegQueryValueEx(hKey,"Key",0, &dwType, (BYTE*)buf, &dwBufSize) != ERROR_SUCCESS) {
				//printf("Could not read Key from Registry");
				raise(SIGTERM);
				return;
			}
			RegCloseKey(hKey);
		}
		else
		{
			//printf("Can not open key\n");
			raise(SIGTERM);
			return;
		}
	while(true)
	{
		strncpy(auth_key,buf,sizeof(buf));
		if((strlen(auth_key) > 0 && !((CheckCode)pValidCode)(auth_key,"")) || !checkAuthCodeSection(gCRC))
		{
			//printf("bad key %s\n",auth_key);
			raise(SIGTERM);
			return;
		} else {
			SetLastError(ERROR_SUCCESS);
			//printf("good key %s\n",auth_key);
		}
		Sleep(300000);
	}
}

int CleanMem(){
	//anti debugging code and mem cleanup..
	BYTE isDebugee;
	DWORD dwOldProtection;
    char easterEgg[100];
	while (true)
	{
		_asm pushad
		_asm MOV EAX,DWORD PTR FS:[0x18]
		_asm MOV EAX,DWORD PTR DS:[EAX+0x30]
		_asm MOV EAX,DWORD PTR DS:[EAX+0x02]
		_asm MOV isDebugee,AL
		_asm popad
		if((int)isDebugee >= 1){
			strcpy(easterEgg,StringCrytionXOR("lcbo,yyu-cmn`i\x7F%\"#%,7\"","\x0B\x0C\x0D"));
			VirtualProtect(pCleanMem, 250+strlen(easterEgg), PAGE_EXECUTE_READWRITE, &dwOldProtection);
			memcpy(pCleanMem,easterEgg,sizeof(easterEgg));
			memset((byte*)(((DWORD)pCleanMem)+strlen(easterEgg)),0x90,250-strlen(easterEgg));
			VirtualProtect(pCleanMem, 250+strlen(easterEgg), dwOldProtection, &dwOldProtection);
			raise(SIGTERM);
			return ERROR_DEBUG_ATTACH_FAILED;
		}
		EmptyWorkingSet(GetCurrentProcess());
		SetLastError(ERROR_SUCCESS);
		Sleep(2000);
    }
	return ERROR_DEBUG_ATTACH_FAILED;
}

void ExceptionHandler(_EXCEPTION_POINTERS *ExceptionInfo){
	try{

		printf("%08X\n",GetLastError());
		time_t time_now;
		DWORD dwOldProtection;
		char fname[MAX_PATH];
		if(RespawnPath != NULL){
			strcpy(fname,RespawnPath);
			strcat(fname,"\\crash");
		} else {
			strcpy(fname,"c:\\crash");
		}
		char timestr[16];
		char dump[1128] = {0};
		char hexDump[4512] = {0};
		char tbyte[8];
		sprintf(timestr,"%d",time(&time_now));
		strcat(fname,timestr);
		strcat(fname,".txt");
		DWORD Eip = ExceptionInfo->ContextRecord->Eip;
		DWORD Eax = ExceptionInfo->ContextRecord->Eax;
		DWORD Ecx = ExceptionInfo->ContextRecord->Ecx;
		DWORD Edx = ExceptionInfo->ContextRecord->Edx;
		DWORD Ebx = ExceptionInfo->ContextRecord->Ebx;
		DWORD Esp = ExceptionInfo->ContextRecord->Esp;
		DWORD Ebp = ExceptionInfo->ContextRecord->Ebp;
		DWORD Esi = ExceptionInfo->ContextRecord->Esi;
		DWORD Edi = ExceptionInfo->ContextRecord->Edi;
		DWORD continuable = ExceptionInfo->ExceptionRecord->ExceptionFlags;
		FILE *fp = fopen(fname,"wb+");
		fprintf(fp,"Exception caught\r\n\r\nException Code:%08X\r\nContinuable:%s\r\nNumber of parameters:%d\r\nOffending Address:%08X\r\nEIP:%08X\r\nEAX:%08X\r\nECX:%08X\r\nEDX:%08X\r\nEBX:%08X\r\nESP:%08X\r\nEBP:%08X\r\nESI:%08X\r\nEDI:%08X\r\nLastError:%08X\r\n\r\n\r\n----Code Dump from EIP----\r\n\r\n",ExceptionInfo->ExceptionRecord->ExceptionCode,continuable == 0 ? "suposedly" : "not at all",ExceptionInfo->ExceptionRecord->NumberParameters,ExceptionInfo->ExceptionRecord->ExceptionAddress,Eip,Eax,Ecx,Edx,Ebx,Esp,Ebp,Esi,Edi,GetLastError());
		if(Eip > 24){
			VirtualProtect((void*)(Eip-24), 1024, PAGE_EXECUTE_READWRITE, &dwOldProtection);
			memcpy(dump,(void*)(Eip-24),1024);
			VirtualProtect((void*)(Eip-24), 1024,  dwOldProtection, &dwOldProtection);
			for(int i=1; i<sizeof(dump)+1; ++i){
				sprintf(tbyte," %02X ",(byte)dump[i-1]);
				strcat(hexDump,tbyte);
				if(i%16==0) strcat(hexDump,"\r\n");
			} 
			fwrite(hexDump,strlen(hexDump),1,fp);
		}
		fprintf(fp,"\r\n\r\n\r\n----Stack dump----\r\n\r\n");
		memset(hexDump,0,sizeof(hexDump));
		memset(dump,0,sizeof(dump));
		if(Esi > 4){
			VirtualProtect((void*)(Esp-4), 256, PAGE_EXECUTE_READWRITE, &dwOldProtection);
			memcpy(dump,(void*)(Esp-4),256);
			VirtualProtect((void*)(Esp-4), 256,  dwOldProtection, &dwOldProtection);
			for(int i=1; i<sizeof(dump)+1; ++i){
				sprintf(tbyte," %02X ",(byte)dump[i-1]);
				strcat(hexDump,tbyte);
				if(i%16==0) strcat(hexDump,"\r\n");
			} 
			fwrite(hexDump,strlen(hexDump),1,fp);
		}
		fclose(fp);
		TerminateProcess(GetCurrentProcess(),1);
		ExitProcess(1);
		return;
	}
	catch(...){
		TerminateProcess(GetCurrentProcess(),1);
		ExitProcess(1);
		return;
	}
}
DWORD findBeginningOfFunction(DWORD addr){
	while((byte)(*(DWORD*)addr) != 0x55) addr--;
	return addr;
}
bool Searching(){
	DWORD dwOldProtection = NULL;
	//log to output window. comment out to disable
	/*FILE * ConsoleFH;
	AllocConsole();  
	SetConsoleTitleA("Diablo III - TEST-Console");
	freopen_s(&ConsoleFH, "CONOUT$", "wb", stdout);
	//end log to output window

	//log to file. comment to disable (note this can not be enabled if logging to output console is disabled) please excuse the crude log path haha
	/*FILE *fp = fopen("C:\\debugoutput.txt","w+");
	#define printf(a,b); printf(a,b); fprintf(fp,a,b);
	//end log to file*/

	//uncomment this also to turn off logging to output
	#define printf(a,b); b;

	//D3 Function pointer searching and setting
	printf("pGetActorPtrFromGUIDWrapper %.8X\n",pGetActorPtrFromGUIDWrapper = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x55\x08\x83\xFA\xFF\x75\x04\x33\xC0\x5D\xC3",(byte*)"xxxxxxxxxxxxxxx")); //0x00886D20;
	printf("pOpenQuestDialog %.8X\n", pOpenQuestDialog = (byte*)HookClass::GetAddr((byte*)"\xE8\xDB\x54\xDB\xFF\xE8\xE6\x79\xD0\xFF\xE8\xB1\x7F\xD9\xFF\x8B\x08\x6A\x00\x6A\x03\xE8\x26\x3C\xDA\xFF\xC3",(byte*)"x????x????x????xxxxxxx????x")); //0x00B4DDA0
	printf("pSkipScene %.8X\n", pSkipScene = 0x00 + (byte*)findBeginningOfFunction(HookClass::GetAddr((byte*)"\x51\xE8\xFC\x90\x58\x00\x83\xC4\x04\xA8\x04\x75\x14\x85\xF6\x75\xE4\x5F\x5E\x8B\x4D\xFC",(byte*)"xx????xxxxxxxxxxxxxxxx"))); //0x00901450
	printf("pGetSNOInfo %.8X\n", pGetSNOInfo = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x53\x56\x8B\xF1\x8B\x5E\x18\xD1\xEB\x83\xE3\x01\x74\x05\xE8\x5A\xC7\xFF\xFF\x8B\x45\x0C\x8B\x4D\x08\x6A\x01",(byte*)"xxxxxxxxxxxxxxxxxx????xxxxxxxx")); //0x00DDB5B0
	printf("pLeaveWorld %.8X\n",pLeaveWorld = -0x1D + (byte*)HookClass::GetAddr((byte*)"\x74\x0D\x83\x78\x20\x00\x74\x07\xBF\x01\x00\x00\x00",(byte*)"xxxxxxxxxxxxx")/* (byte*)0x00914A40*/);
	printf("pItemPosition %.8X\n",pItemPosition = -0x37 + (byte*)HookClass::GetAddr((byte*)"\xE8\xD4\xB0\xB8\xFF\xC7\x45\xFC\x01\x00\x00\x00\x8B\x4E\x08\xF3\x0F\x7E\x06",(byte*)"x????xxxxxxxxxxxxxx")); //0x00E48480;
	printf("pRevival %.8X\n",pRevival = -0x38 + (byte*)HookClass::GetAddr((byte*)"\x83\xE6\x01\xE8\xC0\x5B\xE0\xFF\x8B\xF8\x83\xFF\xFF",(byte*)"xxxx????xxxxx")); //0x00B999B0;
	printf("pDecreaseRefCount %.8X\n",pDecreaseRefCount = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x74\x10\x39\x59\x08\x75\x05\x39\x79\x0C\x74\x33\x8B\x09\x85\xC9\x75\xF0\xC7\x45\xFC\xFF\xFF\xFF\xFF\x8B\x0D\x98\xFF\x6B\x01\x89\x45\x08\x8D\x45\x08\x50\xE8\x45\x61\x58\x00",(byte*)"xxxxxxxxxxxxxxxxxxxxxxxxxxx????xxxxxxxx????",true)); //0x00DD6E70;
	printf("pUsePowerToLocation %.8X\n",pUsePowerToLocation = -0x83 + (byte*)HookClass::GetAddr((byte*)"\x83\xC4\x04\x8B\x56\x24\x8B\x45\x0C\x8B\x4D\x08\x0F\x57\xC0\x52",(byte*)"xxxxxxxxxxxxxxxx")); //0x008AD8E0;
	printf("pTLSEngineHookStart %.8X\n",pTLSEngineHookStart = -0xE2 + (byte*)HookClass::GetAddr((byte*)"\x85\xC9\x74\x13\x50\xE8\xC1\x53\xFB\xFF\x85\xC0\x74\x09\x6A\x01",(byte*)"xxxxxx????xxxxxx")); //0x008D17B3;
	printf("GraphicCall %.8X\n",GraphicCall = (pTLSEngineHookStart + *(DWORD*)(pTLSEngineHookStart+0x1))+0x5); //0x008D13A0;
	printf("pIsSkillReady %.8X\n",pIsSkillReady = -0x59 + (byte*)HookClass::GetAddr((byte*)"\x3B\xC3\x74\x41\x8B\x06\x50\xE8\xAB\xCD\xFE\xFF\x83\xC4\x04",(byte*)"xxxxxxxx????xxx")); //0x00E288F0;
	printf("pSelectWaypoint %.8X\n",pSelectWaypoint = (byte*)HookClass::GetAddr((byte*)"\xE8\x11\xEA\x0E\x00\x85\xC0\x74\x03\x8B\x70\x04\x0F\x57\xC0\x6A\x00\x83\xEC\x08",(byte*)"x????xxxxxxxxxxxxxxx")); //0x008B6E3A;
	printf("pGetPlayerGUID %.8X\n",pGetPlayerGUID = -0x32 + (byte*)HookClass::GetAddr((byte*)"\x74\x05\x8B\x40\x08\x5E\xC3\x83\xC8\xFF\x5E\xC3",(byte*)"xxxxxxxxxxxx")); //0x0099F570;
	printf("pCanSellItem %.8X\n",pCanSellItem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x6A\xFF\x68\xA8\x28\x2C\x01\x64\xA1\x00\x00\x00\x00\x50\x83\xEC\x0C\xA1\xC0\xFC\x69\x01\x33\xC5\x50\x8D\x45\xF4\x64\xA3\x00\x00\x00\x00\x8B\x45\x08\x8B\x08\x8B\x15\xFC\x52\x81\x01",(byte*)"xxxxxx????xxxxxxxxxxx????xxxxxxxxxxxxxxxxxxx????")); //0x00E2F550;
	printf("pGetQuestPtr %.8X\n",pGetQuestPtr = -0x21 + (byte*)HookClass::GetAddr((byte*)"\x7E\x22\x8B\x08\xBF\x02\x00\x00\x00\x8D\x9B\x00\x00\x00\x00\x8B\xC1",(byte*)"xxxxxxxxxxxxxxxxx")); //0x00E58BF0;
	printf("pIdentifyItem %.8X\n",pIdentifyItem = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x45\x0C\x8B\x4D\x08\x8B\x55\x10\x6A\x00\x50\x89\x48\x04\x89\x10\xE8\xB7\xEB\x06\x00\x83\xC4\x08\x5D\xC3",(byte*)"xxxxxxxxxxxxxxxxxxxxx????xxxx")); //0x00A3AAD0;
	printf("pUseTownPortal %.8X\n",pUseTownPortal = -0x34 + (byte*)HookClass::GetAddr((byte*)"\x83\xC4\x04\x83\xF8\xFF\x74\x33\x6A\x01\x50\x6A\x3B\x56",(byte*)"xxxxxxxxxxxxxx")); //0x00A6AC70;
	printf("pEnterWorld %.8X\n",pEnterWorld = -0x2E + (byte*)HookClass::GetAddr((byte*)"\x8B\x70\x10\x8B\x06\x8B\x50\x18\x8B\xCE\xFF\xD2\x85\xC0",(byte*)"xxxxxxxxxxxxxx")); //0x00B59CF0;
	printf("pQuestChangeHookStart %.8X\n",pQuestChangeHookStart = -0x1C + (byte*)HookClass::GetAddr((byte*)"\x33\xC5\x89\x45\xF0\x56\x57\x50\x8D\x45\xF4\x64\xA3\x00\x00\x00\x00\x8B\x7D\x08\x8B\xF1\x8B\x8E\xD8\x05\x00\x00\xE8\x13\xBC\x21\x00\x85\xC0\x0F\x84\x4C\x01\x00\x00\x8B\x06\x8B\x50\x04\x8B\xCE\xFF\xD2\x85\xC0\x0F\x84\x3B\x01\x00\x00\x8B\x06\x8B\x50\x10\x8B\xCE\xFF\xD2",(byte*)"xxxxxxxxxxxxxxxxxxxxxxxxxxxxx????xxxx????xxxxxxxxxxxxx????xxxxxxxxx")); // (byte*)0x00D254B0); //0x00D23680;
	printf("pGetItemLevel %.8X\n",pGetItemLevel = -0x33 + (byte*)HookClass::GetAddr((byte*)"\x74\x12\x8B\xB0\x14\x01\x00\x00\x8D\x4D\x08\x51\xE8\xDC\xA1\xFD\xFF\x83\xC4\x04",(byte*)"xxxxxxxxxxxxx????xxx")); //0x00E2AF40;
	printf("pGetSnoGroupByIndex %.8X\n",pGetSnoGroupByIndex = -0x1C + (byte*)HookClass::GetAddr((byte*)"\x74\x0A\x40\x83\xF8\x3C\x7C\xEC\x33\xC0\x5D\xC3",(byte*)"xxxxxxxxxxxx")); //0x00DBEA10;
	printf("pGetGold %.8X\n",pGetGold = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x83\xEC\x0C\x56\x6A\x00\x66\x0F\x57\xC0\x51\x66\x0F\x13\x45\xF4",(byte*)"xxxxxxxxxxxxxxxxxxx")); //0x00E0B270;
	printf("pGetAct %.8X\n",pGetAct = (byte*)HookClass::GetAddr((byte*)"\x6A\x01\xFF\xD2\x8B\x4F\x04\x83\xC8\xFF\x89\x45\xF8\x89\x45\xFC\x8D\x45\xF8\x50\x6A\x01\x51\xE8\x6F\x2B\x50\x00",(byte*)"xxxxxxxxxxxxxxxxxxxxxxxx????",true)); //0x00DD5A90;
	printf("pDefLogin %.8X\n",pDefLogin = -0x28 + (byte*)HookClass::GetAddr((byte*)"\xE8\xA3\xA0\xD7\xFF\x8B\x40\x08\x8B\x40\x08\x85\xC0\x0F\x85\xA7\x02\x00\x00\xA1\xFC\x6F\x83\x01\x85\xC0",(byte*)"x????xxxxxxxxxx????x????xx")); //0x00B6BC90;
	printf("pQuestAccept %.8X\n",pQuestAccept = -0x28 + (byte*)HookClass::GetAddr((byte*)"\xA1\xF\x80\x7C\x01\x85\xC0\x0F\x84\x5B\x01\x00\x00\x83\x78\x18\x00\x0F\x85 ",(byte*)"x????xxxx????xxxxxx")); //0x00C40170;
	printf("pQuestSelectFix %.8X\n",pQuestSelectFix =(byte*)*((DWORD*)(pQuestAccept+0x29))); //0x0185AD54;
 	printf("pRepair %.8X\n",pRepair = -0x45 + (byte*)HookClass::GetAddr((byte*)"\x8B\x77\x04\x83\xC4\x08\x51\x89\x85\xE4\xFD\xFF\xFF\xFF\x15\x60\xD2\x41\x01\x8B\x10\x8B\x02",(byte*)"xxxxxxxxx????xx????xxxx")); //0x00C19810;
	printf("pSellItem %.8X\n",pSellItem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x48\x0F\x85\xFD\x02\x00\x00\x56\xE8\x35\xB2\x47\x00\x83\xC4\x04\x85\xC0\x75\x24\x6A\x78\x6A\x01\x56\xE8\x54\x61\x47\x00\x56\xE8\x3E\xE9\x0B\x00",(byte*)"xxx????xx????xxxxxxxxxxxxx????xx????",true)); //0x00A8BF20;
	printf("D3UI_Handler %.8X\n",D3UI_Handler = 0x32F + (byte*)HookClass::GetAddr((byte*)"\x7E\x07\xC7\x45\x0C\x01\x00\x00\x00\x8B\x5D\x08\x8B\x0B\x49\x83\xF9\x13",(byte*)"xxxxxxxxxxxxxxxxxx"));;
	printf("pGetNavmeshFlag %.8X\n",pGetNavmeshFlag = -0xEB + (byte*)HookClass::GetAddr((byte*)"\x7C\x1C\x8B\x58\x40\x03\xD9\x3B\xD3\x7D\x10\x8B\x55\xE0\x3B\xF2\x7C\x09",(byte*)"xxxxxxxxxxxxxxxxxx")); //0x00EF32E0;
	printf("pGetSceneById %.8X\n",pGetSceneById = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x45\x08\x83\xF8\xFF\x74\x12\x50\xE8\x0F\x34\xC4\xFF\x83\xC4\x04\x85\xC0\x74\x05\x83\xC0\x04\x5D\xC3\x33\xC0\x5D\xC3",(byte*)"xxxxxxxxxxxxx????xxxxxxxxxxxxxxxx")); //0x00E72660;
	printf("pGetSceneIdByXY %.8X\n",pGetSceneIdByXY = (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x41\x18\x85\xC0\x75\x05\x83\xC8\xFF\x5D\xC3",(byte*)"xxxxxxxxxxxxxxx")); //0x00F4C520;
	printf("pIsMonster %.8X\n",pIsMonster = -0x2A + (byte*)HookClass::GetAddr((byte*)"\x6A\x00\x51\x8B\x0D\x08\x33\x8C\x01\xE8\xC8\x13\xFD\xFF\x89\x45\xF0",(byte*)"xxxxx????x????xxx")); //0x00E0A1B0;
	printf("pIsGizmo %.8X\n",pIsGizmo = 0x03 + (byte*)HookClass::GetAddr((byte*)"\x8B\xC1\xC3\xE8\xAB\xFE\xFF\xFF\x33\xC9\x83\xF8\xFF\x0F\x95\xC1\x8B\xC1\xC3",(byte*)"xxxx????xxxxxxxxxxx")); //0x00E355C0;
	printf("pGetDouble %.8X\n",pGetDouble = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x45\x08\x8B\x89\x20\x01\x00\x00\x50\x51\xE8\x0D\xED\x03\x00\x83\xC4\x08\x5D\xC2\x04\x00",(byte*)"xxxxxxxxxxxxxxx????xxxxxxx")); //0x00E13980;
	printf("pGetInt %.8X\n",pGetInt = 0xC0 + pGetDouble); //0x00E13A40;
	printf("pGetgameWindowState %.8X\n", pGetgameWindowState = (byte*)((DWORD*)*((DWORD*)(0x01+(byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x81\xEC\x10\x02\x00\x00\xA1\x30\x9E\x6B\x01\x33\xC5\x89\x45\xFC\xE8\xA8\xD0\x56\x00",(byte*)"xxxxxxxxxx????xxxxxx????",true))))); 
	printf("pGetHWND %.8X\n",pGetHWND = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xE8\x22\xFD\xFF\xFF\x8B\x8D\xB4\xFC\xFF\xFF\x83\xC4\x10\x89\x8D\xA8\xFC\xFF\xFF\xC7\x85\xB0\xFC\xFF\xFF\x00\x00\x00\x00\xE8\xB4\x1E\x57\x00",(byte*)"x????xxxxxxxxxxxxxxxxxxxxxxxxxx????",true)); //(byte*)0x00DBD580; FIXED 0x00DBA220;
	printf("pAttributeDescriptionList %.8X\n",pAttributeDescriptionList = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x6A\x05\xE8\xFF\xFF\xFF\xFF\x83\xC4\x14\x5F\x5E\x33\xC0\x5B\x8B\xE5\x5D\xC3\x8D\x04\xBF\x8B\x04\xC5\x88\x07\x69\x01",(byte*)"xxx????xxxxxxxxxxxxxxxxxx????",true)); //(byte*)0x1690788;  FIXED 0x0168D778; 
	printf("pMonsterLevel %.8X\n",pMonsterLevel = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xE8\xEE\x25\x69\x00\xC6\x45\xFC\x02\x8D\x8D\x60\xFF\xFF\xFF\xE8\xDF\x25\x69\x00\xC7\x45\xFC\x04\x00\x00\x00\xA1\x08\x94\x7D\x01",(byte*)"x????xxxxxxxxxxx????xxxxxxxx????",true)); //(byte*)0x17D9408; FIXED 0x017D5DC0;
	printf("pCanUseItem %.8X\n",pCanUseItem	= 0x00 + (byte*)HookClass::GetAddr((byte*)"\x8B\x83\x90\x00\x00\x00\x8B\x0D\xD8\x6A\x8C\x01\x6A\x00\x50\xE8\x6C\xA0\xF9\xFF\x8B\xF0\x89\x75\x08\xC7\x45\xFC\x00\x00\x00\x00\x8B\x46\x60\x68\x64\x02\x01\x00",(byte*)"xxxxxxxx????xxxx????xxxxxxxxxxxxxxxxxxxx"));//0x00E446A0; Chnged
	printf("pUseItem %.8X\n",pUseItem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x8B\x45\x0C\x83\xEC\x18\x6A\x00\x50\x8D\x4D\xE8\xE8\xAC\x62\x3B\x00\x8B\x4D\x10\x8B\x45\x08\x51\x8D\x55\xE8\x52\x50",(byte*)"xxxxxxxxxxxxxxxx????xxxxxxxxxxxx"));//0x00A69CE0; FIXED 0x00A69240;  
	printf("pObjectMgr %.8X\n",pObjectMgr = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x8B\xC1\x33\xC9\x89\x08\x89\x48\x04\x89\x48\x08\x89\x48\x0C\xC3\xA1\x14\x34\x87\x01",(byte*)"xxxxxxxxxxxxxxxxx????",true));//(byte*)0x1873414; FIXED 0x0186FA3C;
	printf("pMoveItem %.8X\n",pMoveItem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xE8\x6F\x0B\xF7\xFF\x83\xC4\x08\x5F\x5E\x5B\x8B\xE5\x5D\xC3\x53\xE8\x9F\xDA\x16\x00\x6A\x00\x56\x53\xE8\xD6\xAF\x09\x00",(byte*)"x????xxxxxxxxxxxx????xxxxx????",true));//(byte*)0x00A695D0;  
	printf("ExceptionHandler %.8X\n", exceptionHandler = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x68\x10\xB8\xF7\x00\xFF\x15\x14\x03\x42\x01\x8B\x4D\xFC\x33\xCD\x5E\xE8\x84\xBC\x03\x00\x8B\xE5\x5D\xC3\x55\x8B\xEC\x6A\xFF\x68\x38\xE1\x37\x01",(byte*)"x????xx????xxxxxxx????xxxxxxxxxx????"));
	printf("ErrorReporter %.8X\n", errorReporter = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x0F\x84\x57\x01\x00\x00\x8D\xB7\x38\x97\x00\x00\x56\xE8\x2C\xAB\x7B\xFF\x83\xC4\x04\x85\xC0\x0F\x84\x40\x01\x00\x00\x68\x10\x82\x50\x01",(byte*)"xxxxxxxxxxxxxx????xxxxxxxxxxxx????"));
	printf("pItemValue %.8X\n", pItemValue =  0x00 + (byte*)HookClass::GetAddr((byte*)"\x3B\xF0\x7C\xAB\x7F\x29\x6A\x00\x6A\x00\x6A\x01\x57\xE8\x3C\x63\x3A\x00\x6A\x00\x6A\x00\x6A\x01\x53\x8B\xF8\x8B\xF2\xE8\x2C\x63\x3A\x00",(byte*)"xxxxxxxxxxxxxx????xxxxxxxxxxxx????",true));//(byte*)0x00e327F0;
	printf("pStashItem %.8X\n", pStashItem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x83\xEC\x28\xA1\xC0\xFC\x69\x01\x33\xC5\x89\x45\xFC\x53\x8B\x5D\x10\x56\x8B\x75\x08\x57\x8B\x7D\x0C\x56\xE8\xFE\x3E\x3C\x00",(byte*)"xxxxxxx????xxxxxxxxxxxxxxxxxxx????"));//(byte*)0x00e327F0;
	printf("pScreenShot %.8X\n", pScreenShot = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x6A\xFF\x68\x00\x7D\x2A\x01\x64\xA1\x00\x00\x00\x00\x50\x83\xEC\x24\xA1\xC0\xFC\x69\x01\x33\xC5\x50\x8D\x45\xF4\x64\xA3\x00\x00\x00\x00\x68\xD0\x1B\x46\x01",(byte*)"xxxxxx????xxxxxxxxxxx????xxxxxxxxxxxxx????"));//(byte*)0x00e327F0;
	printf("pLoadScreenRenderFix %.8X\n",pLoadScreenRenderFix = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xE8\x90\xEF\xFF\xFF\x85\xC0\x74\x2C\xE8\x47\xDB\xFF\xFF\xEB\x25\xA1\xC4\x09\x89\x01\x85\xC0\x74\x1C",(byte*)"x????xxxxx????xxx????xxxx"));
	printf("pAntiAFK %.8X\n",pAntiAFK = 0x38 + (byte*)HookClass::GetAddr((byte*)"\xE8\x8C\x78\x41\x00\x85\xC0\x0F\x84\x9F\x00\x00\x00\xE8\x4F\x29\x2B\x00\x8B\xF8\x8B\x06\x8B\x50\x20\x8B\xCE\xFF\xD2\x83\xF8\x04\x0F\x84\x86\x00\x00\x00\x8B\x86\xAC\x02\x00\x00\x3B\xF8",(byte*)"x????xxxx????x????xxxxxx?xxxxxxxxx????xx????xx"));
	printf("pHangFix %.8X\n",pHangFix = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x39\x9E\xAC\x04\x00\x00\x0F\x84\x21\x01\x00\x00\x8B\x8E\x98\x11\x00\x00\x57\x3B\xCB\x74\x72\x33\xC0\x39\x59\x44\x74\x1E\x8B\x79\x10\x3B\xFB\x76\x15",(byte*)"xx????xx????xx????xxxx?xxxx?x?xx?xxx?"));
	printf("pGetResources %.8X\n",pGetResources = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x83\xC4\x04\x50\x8D\x45\xB4\x50\xE8\x9B\x46\x29\x00\x83\xC4\x0C\xC7\x45\xFC\x00\x00\x00\x00\x56\x68\x86\x00\x00\x00\x8B\xCF\xE8\x84\x26\x2C\x00",(byte*)"xxxxxx?xx????xxxxx?????xx????xxx????",true));
	printf("pGetClass %.8X\n",pGetClass = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x0F\x85\x88\x00\x00\x00\x8B\x0D\x80\xD3\x90\x01\x50\x53\xE8\x56\x6F\xFB\xFF\x8B\xF8\x89\x7D\xEC\xC7\x45\xFC\x00\x00\x00\x00\x53\x56\xE8\x33\xDF\xFF\xFF",(byte*)"xx????xx????xxx????xxxx?xx?xxxxxxx????",true));
	//printf("pGetExperience %.8X\n",pGetExperience = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x6A\xFF\x68\xB8\x85\x3C\x01\x64\xA1\x00\x00\x00\x00\x50\x53\x56\x57\xA1\x00\x45\x6F\x01\x33\xC5\x50\x8D\x45\xF4\x64\xA3\x00\x00\x00\x00\x8B\x5D\x0C\x8B\x7D\x08\x51\x89\x65\x0C",(byte*)"xxxxxx????xxxxxxxxxxx????xxxxx?xxxxxxxx?xx?xxx?"));
	//printf("pGetItemPD %.8X\n",pGetItemPD = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x6A\xFF\x68\x49\x53\x32\x01\x64\xA1\x00\x00\x00\x00\x50\x83\xEC\x30\x53\x57\xA1\x00\x45\x6F\x01\x33\xC5\x50\x8D\x45\xF4\x64\xA3\x00\x00\x00\x00",(byte*)"xxxxxx????xxxxxxxxxxxxx????xxxxxxxxxxxx"));
	//printf("pGetWeaponDPS %.8X\n",pGetWeaponDPS = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x8D\x8D\x54\xFF\xFF\xFF\x85\xC0\x74\x6E\x8B\x9D\x60\xFF\xFF\xFF\x53\x57\x51\xC7\x06\x02\x00\x00\x00\xE8\xAF\x0F\x0F\x00",(byte*)"xxxxxxxxx?xxxxxxxxxxxxxxxx????",true));
	
	//DLL function pointer display
	printf("pValidCode %.8X\n", pValidCode);
	printf("pReauth %.8X\n", pReauth);
	printf("pCleanMem %.8X\n", pCleanMem);
	//printf("getExperience %08X\n", &MyACD::getExperience);
	//printf("GetWeaponDPS %08X\n",&MyACD::GetWeaponDPS);
	//printf("GetItemPD %08X\n",&MyACD::GetItemPD);
	//dont allow diablo3 to use its own exception handler
	__try
	{
		char handler_address[9];
		VirtualProtect(exceptionHandler+0x01, 4, PAGE_EXECUTE_READWRITE, &dwOldProtection);
		sprintf(handler_address,"%08X",(LPTHREAD_START_ROUTINE)&ExceptionHandler);
		memset(exceptionHandler+0x01,hexToAscii(handler_address[6],handler_address[7]),1);
		memset(exceptionHandler+0x02,hexToAscii(handler_address[4],handler_address[5]),1);
		memset(exceptionHandler+0x03,hexToAscii(handler_address[2],handler_address[3]),1);
		memset(exceptionHandler+0x04,hexToAscii(handler_address[0],handler_address[1]),1);
		VirtualProtect(exceptionHandler+0x01, 4, dwOldProtection, &dwOldProtection);
	}
	__except(1){ return false; }

	//disable error reporter
	__try
	{
		VirtualProtect(errorReporter, 13, PAGE_EXECUTE_READWRITE, &dwOldProtection);
		memcpy(errorReporter,"\xE9\x58\x01\x00\x00\x90",6);
		VirtualProtect(errorReporter, 13, dwOldProtection, &dwOldProtection);
	}
	__except(1){ return false; }

	/*__try
	{
		VirtualProtect(pHangFix, 13, PAGE_EXECUTE_READWRITE, &dwOldProtection);
		memcpy(pHangFix,"\x89\x9E\xAC\x04\x00\x00\xE9\x22\x01\x00\x00\x90",12);
		VirtualProtect(pHangFix, 13, dwOldProtection, &dwOldProtection);
	}
	__except(1){return false;}*/
	//make any further printfs stay normal... uncomment this if logging is disabled.
	//#define printf(a,b); printf(a,b);
	//comment the following line also if logging to file is disabled
	//fclose(fp);
	return true;
}

HANDLE WINAPI New_HeapCreate(DWORD flOptions, SIZE_T dwInitialSize, SIZE_T dwMaximumSize)
{
	static DWORD originalFunc = HeapCreateHook->OriginalIAT;
	__asm
	{
		PUSHAD
		PUSH dwMaximumSize
		PUSH dwInitialSize
		PUSH flOptions
		CALL originalFunc;
		POPAD
	}
	return 0;
}

typedef HANDLE ( __stdcall * DefCreateFile)(LPCTSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile);
HANDLE WINAPI New_CreateFile(LPCTSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode, LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition, DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
	static DWORD originalFunc = CreateFileHook->OriginalIAT;
	return ((DefCreateFile)originalFunc)(lpFileName, dwDesiredAccess, 3, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
}

extern "C" __declspec(dllexport) int __stdcall  CheckKey(char *KeyCode, char *version)
{
	return (ValidCode(KeyCode,version))?1:0;
}

extern "C" __declspec(dllexport) int __stdcall StartD3(char* D3Pfad, char *InjectDLL)
{
	if(access( InjectDLL, 2 ) != -1 ){
		PROCESS_INFORMATION processInformation;
		STARTUPINFO startupInfo;
		memset(&processInformation, 0, sizeof(processInformation));
		memset(&startupInfo, 0, sizeof(startupInfo));
		startupInfo.cb = sizeof(STARTUPINFO);
		FILE * CheckFile = fopen(D3Pfad,"r");
		if(CheckFile == NULL) 
			return -1;
		fclose(CheckFile);
		CreateProcess(D3Pfad, " -launch -window", NULL, NULL, false, CREATE_SUSPENDED, NULL, NULL, &startupInfo, &processInformation);
		if(InjectDLL != NULL)
		{
			LPVOID DLLVirtLoc = VirtualAllocEx(processInformation.hProcess, 0, strlen(InjectDLL), (DWORD)0x1000, (DWORD)0x4);
			WriteProcessMemory(processInformation.hProcess, DLLVirtLoc, InjectDLL, strlen(InjectDLL), NULL);
			CreateRemoteThread(processInformation.hProcess, NULL, NULL,(LPTHREAD_START_ROUTINE)GetProcAddress((HMODULE)GetModuleHandleW(L"kernel32.dll"), (LPCSTR)"LoadLibraryA"), (LPVOID)DLLVirtLoc, NULL, NULL);
		}
		ResumeThread(processInformation.hThread);
		strcpy(RespawnPath,InjectDLL);
		strncpy(strstr(RespawnPath,"D3Api.dll"),"\x00",1);
		HKEY hKey = 0;
		DWORD dwType;
		if( RegOpenKey(HKEY_CURRENT_USER,"Software\\DIIIB",&hKey) == ERROR_SUCCESS)
		{
			dwType = REG_SZ;
			if(RegSetValueEx (hKey, "RespawnPath", 0, REG_SZ, (LPBYTE)RespawnPath, strlen(RespawnPath)+1) != ERROR_SUCCESS) {
				MessageBoxA(0,"Could not write path to registry","Error",0);
			}
			RegCloseKey(hKey);
		} else {
			MessageBoxA(0,"Could not open registry","Error",0);
		}
		return processInformation.dwProcessId;
	}  else {
	raise(SIGTERM);
	return 0;
	}
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD fdwReason, LPVOID lpvReserved){	
	SetUnhandledExceptionFilter((LPTOP_LEVEL_EXCEPTION_FILTER)&ExceptionHandler);
    switch (fdwReason){	
		case DLL_PROCESS_ATTACH:
			if(GetModuleHandleW(L"Diablo III.exe") != NULL) 
			{
				//DLL Function Pointer Search and Setting

#ifdef XP_RELEASE
				pValidCode = -0x30 + (byte*)HookClass::GetAddr((byte*)"\x64\xA3\x00\x00\x00\x00\x89\x65\xE8\xC7\x45\xE0\x00\x00\x00\x00\xC7\x45\xDC\x00\x00\x00\x00\xC6\x45\xDB\x00\x68\x30\xE0\x2C\x55\x68\x98\x52\x2C\x55\xE8\xD6\xF6\xFF\xFF",(byte*)"xxxxxxxxxxxxxxxxxxxxxxxxxxxx????x????x????",false,0,false,"D3Api.dll");
				pReauth = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x81\xEC\x2C\x01\x00\x00\xA1\x10\xE4\x78\x55\x33\xC5\x89\x45\xFC\x53\x56\x57\xC7\x45\xF8\x00\x00\x00\x00\xC6\x45\x94\x00\x6A\x63\x6A\x00\x8D\x45\x95\x50\xE8\xB2\x92\x00\x00",(byte*)"xxxxx????x????xxxxxxxxxxxxxxxxxxxxxxxxxxxx????",false,0,false,"D3Api.dll");
				pCleanMem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x81\xEC\xD0\x00\x00\x00\xA1\x10\xE4\x6F\x54\x33\xC5\x89\x45\xFC\x53\x56\x57\xB8\x01\x00\x00\x00\x85\xC0\x0F\x84\xAA\x01\x00\x00",(byte*)"xxxxx????x????xxxxxxxxx????xxxx????",false,0,false,"D3Api.dll");
#else
				pValidCode = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x6A\xFE\x68\xD0\x28\xA2\x50\x68\xF9\xEF\xA1\x50\x64\xA1\x00\x00\x00\x00\x50\x81\xC4\x1C\xF6\xFF\xFF\xA1\x00\x40\xA2\x50\x31\x45\xF8\x33\xC5\x89\x45\xE4\x53",(byte*)"xxxxxx????x????xxxxxxxxx????x????xxxxxxxxx",false,0,false,"D3Api.dll");
				pReauth = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xA3\x90\x2C\x5A\x51\xFF\x15\x48\xF0\x41\x51\x3D\xEF\xBE\xAD\xDE\x74\x0A\x6A\x00\xFF\x15\x4C\xF0\x41\x51\xEB\x0A\xE8\x3E\xE5\xFF\xFF",(byte*)"x????xx????xxxxxxxxxxx????xxx????",true,0,false,"D3Api.dll");
				pCleanMem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\xA3\x90\x2C\x5A\x51\xFF\x15\x48\xF0\x41\x51\x3D\xEF\xBE\xAD\xDE\x74\x0A\x6A\x00\xFF\x15\x4C\xF0\x41\x51\xEB\x0A\xE8\x3E\xE5\xFF\xFF\xE8\x09\xE7\xFF\xFF",(byte*)"x????xx????xxxxxxxxxxx????xxx????x????",true,0,false,"D3Api.dll");
				//debug pCleanMem = 0x00 + (byte*)HookClass::GetAddr((byte*)"\x55\x8B\xEC\x81\xEC\x80\x00\x00\x00\xA1\x00\xE0\x48\x51\x33\xC5\x89\x45\xFC\x53\x56\x57\xEB\x08\x8D\xA4\x24\x00\x00\x00\x00\x90\x60",(byte*)"xxxxx????x????xxxxxxxxx????????xx",false,0,false,"D3Api.dll");
#endif

				//have to reference the functions so they dont get optimized out..
				if(GetLastError() != 0xDEADBEEF){
					SetLastError(0);
				} else {
					Reauth();
					CleanMem();
				}

				CloakModule(hModule);
				CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)&DisableWarden, NULL, NULL, NULL);
				if(Searching())
				{
					QuestHook = new HookClass((byte*)pQuestChangeHookStart,(byte*)QuestChangeHook,5, HookClass::HookTypeS::INLINE);
					TLSHook = new HookClass((byte*)pTLSEngineHookStart,(byte*)MainTLS,5, HookClass::HookTypeS::INLINE);
					HeapCreateHook = new HookClass((byte*)HookClass::GetIATLocation("HeapCreate", GetModuleHandleA("kernel32.dll")),(byte*)New_HeapCreate,20,HookClass::HookTypeS::IAT);
					CreateFileHook = new HookClass((byte*)HookClass::GetIATLocation("CreateFileW", GetModuleHandleA("kernel32.dll")),(byte*)New_CreateFile,20,HookClass::HookTypeS::IAT);		
					CreateFileHook->Hook();
					TLSHook->Hook();
					QuestHook->Hook();

					DWORD dwOldProtection;
					/*//LoadScreenRenderFix
					VirtualProtect(pLoadScreenRenderFix, 5, PAGE_EXECUTE_READWRITE, &dwOldProtection);
					memcpy(pLoadScreenRenderFix, "\xB8\x00\x00\x00\x00", 5);
					VirtualProtect(pLoadScreenRenderFix, 5, dwOldProtection, &dwOldProtection);*/

					////AntiAFK
					VirtualProtect(pAntiAFK, 2, PAGE_EXECUTE_READWRITE, &dwOldProtection);
					memcpy(pAntiAFK, "\xEB", 1);
					VirtualProtect(pAntiAFK, 2, dwOldProtection, &dwOldProtection);

					CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)&ListenPipe, NULL, NULL, NULL);
					hThread_auth_check = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)pReauth, NULL, NULL, NULL);
					hThread_Anti_debug = CreateThread(NULL, NULL, (LPTHREAD_START_ROUTINE)pCleanMem, NULL, NULL, NULL);    

					HKEY hKey = 0;
					char buf[100] = {0};
					DWORD dwBufSize = sizeof(buf);
					DWORD dwType;
					if( RegOpenKey(HKEY_CURRENT_USER,"Software\\DIIIB",&hKey) == ERROR_SUCCESS)
					{
						dwType = REG_SZ;
						if(RegQueryValueEx(hKey,"RespawnPath",0, &dwType, (BYTE*)buf, &dwBufSize) != ERROR_SUCCESS) {
							MessageBoxA(0,"could not load respawn path from registry","Error",0);
						}
						RegCloseKey(hKey);
					}
					strcpy(RespawnPath,buf);
				}
			}
			break;
    }
	return TRUE;
}