#include <windows.h>
#include<stdio.h>
#include <math.h>
#include <Time.h>
#include <tlhelp32.h>
#include <signal.h>
#include <Wininet.h>
#include <Psapi.h>
#include <io.h>
#pragma comment( lib, "Wininet.lib" )

char OffsetAntiHackCheck[4] = {'X','0','9','X'};
HANDLE hThread_auth_check,hThread_Anti_debug = NULL;
char lootTableBuffer[1048576] = {0x00};
int lootTableSize = 0;
char itemIdsInLootTable[524288] = {0x00};
char RespawnPath[MAX_PATH] = {0x00};
int potionLevel;
char *potionType[] = {"none","Minor","Lesser","Normal","Greater","Large","Super","Heroic","Resplendent","Runic","Mythic"};

char spellrulesbuffer[4096];

char XORKey[]= "\n\t";
char SendXORKey[]= "res";

//function prototypes for functions that are declared in namedpipe.h
void SendRandomlyTimedEnter();

class HookClass
{
private:
	bool _isHooked;
public:
	HookClass(){}
	int Size;
	DWORD OriginalIAT;
	byte* NewFunction;
	byte* SaveOldLocation;
	byte* HookLocation;
	enum HookTypeS
	{
		IAT,
		EAT,
		INLINE,
	}HookType;
	bool isHooked() { return _isHooked; }
	static DWORD* GetIATLocation(const char* targetFunction, HMODULE module)
	{
		PIMAGE_DOS_HEADER pBaseImage = NULL; 
		pBaseImage = (PIMAGE_DOS_HEADER)module;

		if (pBaseImage->e_magic == IMAGE_DOS_SIGNATURE)
		{
			PIMAGE_NT_HEADERS pHeaders = NULL;
			PIMAGE_IMPORT_DESCRIPTOR pImportDescriptor = NULL;
			pHeaders = (PIMAGE_NT_HEADERS)(pBaseImage->e_lfanew + (DWORD)pBaseImage);
			pImportDescriptor = (PIMAGE_IMPORT_DESCRIPTOR)((DWORD)((void*)pHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress) + (DWORD)pBaseImage);

			while (*(DWORD*)pImportDescriptor) 
			{
				PIMAGE_THUNK_DATA32 pImportNameTable = NULL;
				PIMAGE_THUNK_DATA32 pImportAddressTable = NULL;
				pImportNameTable = (PIMAGE_THUNK_DATA32)(pImportDescriptor->OriginalFirstThunk + (DWORD)pBaseImage);
				pImportAddressTable = (PIMAGE_THUNK_DATA32)(pImportDescriptor->FirstThunk + (DWORD)pBaseImage);
				
				while (*(DWORD*)pImportNameTable) 
				{
					PIMAGE_IMPORT_BY_NAME pImportByName = NULL;
					pImportByName = (PIMAGE_IMPORT_BY_NAME)(pImportNameTable->u1.AddressOfData + (DWORD)pBaseImage);

					if (!_stricmp((char*)pImportByName->Name, targetFunction))
						return &(pImportAddressTable->u1.Function);

					++pImportNameTable;
					++pImportAddressTable;
				}
				++pImportDescriptor;
			}
		}
		return NULL;
	}
	HookClass(byte*HookLocation, byte*NewFunction, int Size, HookTypeS HookType)
	{
		this->SaveOldLocation = new byte[Size]; 
		this->HookLocation = HookLocation;
		this->NewFunction = NewFunction;
		this->Size = Size;
		this->HookType = HookType;
		this->_isHooked = false;
	}
	bool Hook()
	{
		if(this->isHooked()) return false;
		DWORD dwOldProtection = NULL;
		if(!VirtualProtect(this->HookLocation, this->Size, PAGE_EXECUTE_READWRITE, &dwOldProtection)){
			//printf("hook protection failed\n");
			return false;
		}
		switch(this->HookType)
		{
			case IAT:
				this->OriginalIAT = *(DWORD*)this->HookLocation;
				*(DWORD*)this->HookLocation = (DWORD)this->NewFunction;
				break;
			case EAT:
				break;
			case INLINE:
				if(!memcpy(SaveOldLocation,HookLocation,this->Size) || !memset(HookLocation,0x90,this->Size)){
					//printf("clear failed\n");
					return false;
				}
				HookLocation[0] = 0xE9;
				*((DWORD*)(HookLocation + 1)) = (DWORD)(this->NewFunction - this->HookLocation) - 5;
				break;
		}
		if(!VirtualProtect(HookLocation, this->Size, dwOldProtection, &dwOldProtection)){
			//printf("reprotect failed\n");
			return false;
		}
		_isHooked = true;
		return true;

	}
	bool UnHook()
	{
		if(!this->isHooked()) return false;
		DWORD dwOldProtection = NULL;
		if(!VirtualProtect(this->HookLocation, this->Size, PAGE_EXECUTE_READWRITE, &dwOldProtection))
			return false;
		switch(this->HookType)
		{
			case IAT:
				*(DWORD*)this->HookLocation = this->OriginalIAT;
				break;
			case EAT:
				break;
			case INLINE:
				if(!memcpy(this->HookLocation,this->SaveOldLocation,this->Size))
					return false;
				break;
		}
		if(!VirtualProtect(HookLocation, this->Size, dwOldProtection, &dwOldProtection))
			return false;
		_isHooked = false;
		return true;
	}
	static DWORD GetAddr(byte *Pattern, byte *Mask, bool by_ref = false, int subtract = 0, bool allMem = false, char* module=NULL)
	{
		DWORD dwModuleBase = (DWORD)GetModuleHandleA( module );
		PIMAGE_NT_HEADERS pNTHeaders = ( PIMAGE_NT_HEADERS )( dwModuleBase + ((PIMAGE_DOS_HEADER )dwModuleBase)->e_lfanew );
		dwModuleBase += pNTHeaders->OptionalHeader.BaseOfCode;


		byte *startScan = (byte*)dwModuleBase;
		byte *endScan = (byte*)((DWORD)(dwModuleBase + (DWORD)pNTHeaders->OptionalHeader.SizeOfCode));
		if(allMem) {
			byte *endScan = (byte*)(((DWORD)(dwModuleBase + (DWORD)pNTHeaders->OptionalHeader.SizeOfCode) + (DWORD)pNTHeaders->OptionalHeader.SizeOfInitializedData));
		}
		while(startScan != endScan)
		{
			for(int i=0; Mask[i] != NULL;++i)
			{
				if(startScan[i] != (Pattern[i]-subtract) && Mask[i] == 'x')
					break;
				if(Mask[i+1]==NULL){
					//if by_ref is selected it will search not for a function signature but a place that the needed offset is referenced within a separate function in D3
					if(by_ref)
					{
						byte address_bytes[] = {0x00,0x00,0x00,0x00};
						address_bytes[3] = startScan[i-3];
						address_bytes[2] = startScan[i-2];
						address_bytes[1] = startScan[i-1];
						address_bytes[0] = startScan[i];
						DWORD address = ((address_bytes[0] << 24) | (address_bytes[1] << 16) | (address_bytes[2]<<8)) | address_bytes[3]; 
						if(startScan[i-4] == 0xE8){
							return (DWORD)((startScan+address)+i+1);
						} else {
						return (DWORD)address;
						}
					} else {
						return (DWORD)startScan;
					}
				}
			}
			++startScan;
		}
		return 0;
	}
};


int DiabloIIICount()
{
  HANDLE hProcessSnap;
  PROCESSENTRY32 pe32;
  int CountD3 = 0;
  hProcessSnap = CreateToolhelp32Snapshot( TH32CS_SNAPPROCESS, 0 );
  if( hProcessSnap == INVALID_HANDLE_VALUE )
    return -1;
  pe32.dwSize = sizeof( PROCESSENTRY32 );
  if( !Process32First( hProcessSnap, &pe32 ) )
  {
    CloseHandle( hProcessSnap );
    return -1;
  }
  do
  {
	  if(!strcmp(pe32.szExeFile,"Diablo III.exe"))
		++CountD3;
  } while( Process32Next( hProcessSnap, &pe32 ) );

  CloseHandle( hProcessSnap );
  return CountD3;
}

char* StringCrytionXOR(char *Text, char *Key)
{
	char *NewText = new char[strlen(Text)+1];
	for(int x=0; x < strlen(Text); ++x)
	{
		NewText[x]=Text[x] ^ Key[x % strlen(Key)];
	}
	NewText[strlen(Text)] = '\0';
	return NewText;
}

char* clean_text(char *txt){
	char *new_txt = new char[((strlen(txt)*2)+1)];
	memset(new_txt,0x00,sizeof(new_txt));
	char buf[16]={0};
	for(int i=0; i<strlen(txt);++i){
		sprintf(buf,"%02X\x00",txt[i]);
	    strcat(new_txt,buf);
	}
	new_txt[strlen(txt)*2] = 0x00;
	return new_txt;
}

DWORD getSystemDriveSerial()
{
    char sysdir[MAX_PATH] = {0};
    char drive[4] = {0};
    DWORD serialnumber;
    GetSystemDirectory(sysdir,MAX_PATH);
    strncpy(drive,sysdir,3);
    GetVolumeInformation(drive,NULL,NULL,&serialnumber,NULL,NULL,NULL,NULL);
    return serialnumber;
}

bool ValidCode(char *MyCode, char *version = "") 
{	
	HINTERNET hSession = NULL, hService = NULL;
	bool Success = false;
	// "b}~y0&%?>'=0$8;8$=?&K|~aZAZ'zaz6K{m97,y/K{m87,y/K{m;7,y/K{m:7,y" stor

	char *Decrypted = StringCrytionXOR("b}~y0&%h\x7F}b:${ozzh}gom$|y&k|~a%h\x7F}b'zaz6K{m97,y/K{m87,y/K{m;7,y/K{m:7,y/K{m=7,y", XORKey);
	char *Decrypted2 = StringCrytionXOR("b}~y0&%h\x7F}b;${ozzh}gom$|y&k|~a%h\x7F}b'zaz6K{m97,y/K{m87,y/K{m;7,y/K{m:7,y/K{m=7,y", XORKey);

	#define MAX_DATA_TO_STREAM 1024*1024 /*max 1MB*/
	char sActServerURL[2][1024];
	char PCUser[100] = {0};
	char PCName[100] = {0};
	char HWSN[16]   = {0};
	sprintf(HWSN,"%08X",getSystemDriveSerial());
	DWORD size = 100;
	GetComputerName(PCName, &size);
	size = 100;
	GetUserName(PCUser, &size); 

	DWORD dwBytesRead = NULL;
	DWORD m_dwTotalBytesRead = NULL;
	byte* m_lpbtStreamedData = NULL;
	int tries = 0;
	char *clean_code = clean_text(StringCrytionXOR(MyCode,SendXORKey));
	sprintf(sActServerURL[0], Decrypted, clean_code, clean_text(StringCrytionXOR(PCUser,SendXORKey)), clean_text(StringCrytionXOR(PCName,SendXORKey)), HWSN,clean_text(StringCrytionXOR(version,SendXORKey)));
	sprintf(sActServerURL[1], Decrypted2, clean_code, clean_text(StringCrytionXOR(PCUser,SendXORKey)), clean_text(StringCrytionXOR(PCName,SendXORKey)), HWSN,clean_text(StringCrytionXOR(version,SendXORKey)));
	while(Success == false && tries < 4) {
	hSession = InternetOpenA(NULL, INTERNET_OPEN_TYPE_PRECONFIG, NULL, NULL, NULL );	
	if( hSession)
	{
		hService = InternetOpenUrlA( hSession, sActServerURL[(tries%2)], NULL, NULL, NULL, NULL );
		DWORD statCharLen = 128;
		char statChar[128];
		HttpQueryInfo(hService, HTTP_QUERY_STATUS_CODE, &statChar,&statCharLen, NULL);

		if(hService && atoi(statChar) == HTTP_STATUS_OK)
		{
			m_lpbtStreamedData = new BYTE[MAX_DATA_TO_STREAM];
			while( InternetReadFile( hService, m_lpbtStreamedData, MAX_DATA_TO_STREAM, &dwBytesRead ) && dwBytesRead != NULL )
			{
				m_lpbtStreamedData[ dwBytesRead ] = '\0';
				m_dwTotalBytesRead += dwBytesRead;
			}
			InternetCloseHandle( hSession );
			InternetCloseHandle( hService );
			char *DecryptedStream = StringCrytionXOR((char*)m_lpbtStreamedData, XORKey);
			if(!memcmp(StringCrytionXOR(StringCrytionXOR(OffsetAntiHackCheck,XORKey),XORKey),DecryptedStream, 4)) 
			{
				Success = ((DiabloIIICount() > 1 && MyCode[1] == '0') || DiabloIIICount() <= 1);
				strcpy(MyCode, DecryptedStream);
			}
			__try{
			memset(DecryptedStream,0,sizeof(DecryptedStream));
			memset(m_lpbtStreamedData,0,sizeof(m_lpbtStreamedData));
			memset(HWSN,0,sizeof(HWSN));
			memset(PCUser,0,strlen(PCUser));
			memset(PCName,0,strlen(PCName));
			memset(Decrypted,0,strlen(Decrypted));
			memset(Decrypted2,0,strlen(Decrypted2));
			}
			__except(1){ continue; }
		}
	}
	++tries;
	if(!Success) Sleep(1000);
	}
	//set all our stuff in memory to 0's
	memset(sActServerURL,0,sizeof(sActServerURL));
	return Success;
}

//D3 Function pointers
HookClass *QuestHook = NULL;
HookClass *HeapCreateHook = NULL;
HookClass *CreateFileHook = NULL;
HookClass *TLSHook = NULL;

DWORD GraphicFlag = NULL;
byte *pSelectWaypoint = NULL;
byte *pObjectMgr = NULL;
byte *pTLSEngineHookStart = NULL;
byte *GraphicCall = NULL;
byte *pAttributeDescriptionList = NULL;
byte *pUsePowerToLocation = NULL;
byte *pGetgameWindowState =NULL;                  
byte *pQuestChangeHookStart = NULL;
byte *D3UI_Handler = NULL;
byte* pGetSnoGroupByIndex = NULL;
byte* pIsSkillReady = NULL;
byte* pGetQuestPtr = NULL;
byte* pGetActorPtrFromGUIDWrapper = NULL;
byte* pUseTownPortal = NULL;
byte* pGetDouble = NULL;
byte* pGetInt = NULL;
byte* pGetSNOInfo = NULL;				
byte* pGetItemLevel = NULL;
byte* pItemPosition = NULL;
byte* pIdentifyItem = NULL;
byte* pCanSellItem =NULL;
byte* pRevival = NULL;
byte* pSkipScene = NULL; 
byte* pRepair = NULL;
byte* pSellItem = NULL;
byte* pLeaveWorld = NULL;
byte* pEnterWorld = NULL;
byte* pOpenQuestDialog =NULL;
byte* pQuestAccept = NULL; 
byte *pQuestSelectFix = NULL;
byte* pGetPlayerGUID = NULL;
byte* pGetGold = NULL; 
byte* pGetAct = NULL;
byte* pGetNavmeshFlag = NULL;
byte* pGetSceneById = NULL;
byte* pGetSceneIdByXY = NULL;
byte* pDecreaseRefCount = NULL;
byte* pIsMonster =NULL;
byte* pIsGizmo =NULL;
byte* pMonsterLevel =NULL;
byte* pCanUseItem =NULL;
byte* pUseItem =NULL;
byte* pMoveItem = NULL;
byte* pItemValue = NULL;
byte* exceptionHandler = NULL;
byte* errorReporter = NULL;
byte* pStashItem = NULL;
byte* pScreenShot = NULL;
byte* pLoadScreenRenderFix = NULL;
byte* pAntiAFK = NULL;
byte* pHangFix = NULL;
byte* pGetResources = NULL;
byte* pGetClass = NULL;
byte* pGetWeaponDPS = (byte*)0x00BFC1E0;


//DLL function pointers
byte* pValidCode = NULL;
byte* pReauth = NULL;
byte* pCleanMem = NULL;



typedef HWND ( __cdecl * GetHWND)();			byte* pGetHWND = NULL;
typedef void ( __cdecl * DefLogin)();			byte* pDefLogin = NULL;


class SceneClass
{
public:
	enum NavCellFlags
	{
		AllowWalk = 0x1,
		AllowFlier = 0x2,
		AllowSpider = 0x4,
		LevelAreaBit0 = 0x8,
		LevelAreaBit1 = 0x10,
		NoNavMeshIntersected = 0x20,
		NoSpawn = 0x40,
		Special0 = 0x80,
		Special1 = 0x100,
		SymbolNotFound = 0x200,
		AllowProjectile = 0x400,
		AllowGhost = 0x800,
		RoundedCorner0 = 0x1000,
		RoundedCorner1 = 0x2000,
		RoundedCorner2 = 0x4000,
		RoundedCorner3 = 0x8000
	};
	struct sScene
	{ 
		DWORD Scene_ID;
		DWORD World_ID;
		DWORD UNK_ID0;
		DWORD UNK0[33];
		DWORD SizeX;
		DWORD SizeY;
		DWORD UNK_0[17];
		DWORD SNO_ID;
		DWORD UNK3[4];
		float PositionX;
		float PositionY;
		DWORD UNK5[33];
		struct sNavMesh
		{
			DWORD SizeX;
			DWORD SizeY;
			DWORD UNK;
			DWORD Size;
		} *NavMesh;
		DWORD UNK4[3];
		DWORD isAktiveFlag;
	};
	static int GetMaxScene()
	{
		__try 
		{
			return *(DWORD*)(*(DWORD*)(*(DWORD*)pObjectMgr+0x8F4)+0x108);
		}__except(1) {
			return NULL;
		}
	}
	static int Scene_NavmeshFlag(sScene::sNavMesh *Navmesh, signed int param)
	{
		typedef int ( __cdecl * GetNavmeshFlag)(sScene::sNavMesh *Navmesh, signed int param);
		return ((GetNavmeshFlag)pGetNavmeshFlag)(Navmesh, param);
	}	
	static int Scene_isValid(unsigned int aIndex)
	{
		return ((GetScene(aIndex))?(GetScene(aIndex)->NavMesh != 0):0);
	}	
	static int Scene_isActive(unsigned int aIndex)
	{
		return ((GetScene(aIndex))?(GetScene(aIndex)->isAktiveFlag & 1):0);
	}	
	static char* Scene_Name(unsigned int aIndex)
	{
		char *SceneName = new char[50];
		
		typedef int ( __thiscall * GetSNOInfo)(int SNOList, int SNO_ID, int zero);
		typedef int ( __thiscall * DecreaseRefCount)(int SNOList, DWORD *GetSNOInfoRetPtr);
		DWORD ScenePtr = GetGroupListPtrByName("Scene");
		if(!ScenePtr) return 0;
		DWORD InfoStructPtr = ((GetSNOInfo)pGetSNOInfo)(ScenePtr, GetScene(aIndex)->SNO_ID,0);
		if(!InfoStructPtr) return NULL;
		char *rName = (char*)(InfoStructPtr+0x68);		
		int DotIndex = 0;
		int SlashIndex = 0;
		for(int i=strlen(rName);i>0;i--)
		{
			if(rName[i] == '.')
			{
				DotIndex = i;
			}
			if(rName[i] == '/')
			{
				SlashIndex = i+1;
				break;
			}
		}
		memcpy(SceneName,(rName+SlashIndex),DotIndex-SlashIndex);
		SceneName[(DotIndex-SlashIndex)] = NULL;
		((DecreaseRefCount)pDecreaseRefCount)(ScenePtr,&InfoStructPtr);
		return SceneName;
	}	
	static sScene *GetScene(unsigned int aIndex)
	{
		if(aIndex > GetMaxScene()){
			return NULL;
		}
		__try 
		{
			return (sScene*)(*(DWORD*)(*(DWORD*)(*(DWORD*)pObjectMgr+0x8F4)+0x148)+(aIndex*0x2A8));
		}__except(1) {
			return NULL;
		}
	}	
	static DWORD GetGroupListPtrByName(const char *Name)
	{	
		int *SnoList = (int*)(*(DWORD*)(pGetSnoGroupByIndex+0x13));
		for(int i=0;i<57;++i)
		{
			if(!SnoList[2*i]) continue;
			if(!strcmp(Name, (char*)(*(DWORD*)SnoList[2*i] + 0x1C)))
				return *(DWORD*)SnoList[2*i];
		}
		return 0;
	}


	static int Scene_GetSceneById(unsigned int aIndex)
	{
		typedef int ( __cdecl * GetSceneById)(int ID);		
		return ((GetSceneById)pGetSceneById)(aIndex);
	}	
	static int Scene_GetSceneIdByXY(unsigned int aIndex)
	{
		typedef signed int ( __thiscall * GetSceneIdByXY)(int ECX, int zero1, int tree, int zero2);
		return 0;
	}
};

class UIElements
{
public:
	struct UIElementStruct
    {
		int *eventHandlersPtr;
        int int_0;
        int int_1;
        int int_2;
        int int_3;
        int int_4;
        int int_5;
        int int_6;
        int int_7;
        int int_8;
        int visible; 
        int int_10; // 0x28
        unsigned long hash; // 0x30
		unsigned long UNK; // 0x30
        char name[0x100]; //130
        int byte_1[0x109]; // 0x554
        int intptr_1;
        int int_11[352];
        char* pntrText;
        int int_12[0x4e];
        int int_13;
    };
	static UIElementStruct* GetUI(const char *Label)
	{
		__try
		{
			int counter = 0;
			int uielemePointer = 0;
			int nPnt = NULL;
			//printf("MC:%08X\n",pObjectMgr);
			DWORD MaxCount = *(DWORD*)pObjectMgr;
			//printf("MC:%08X\n",MaxCount);
			if(!MaxCount) return NULL;
			//printf("MC:%08X\n",MaxCount+0x974);
			MaxCount = *(DWORD*)(MaxCount+0x974);
			//printf("MC:%08X\n",MaxCount);
			if(!MaxCount) return NULL;
			//printf("MC:%08X\n",MaxCount+0x00);
			MaxCount = *(DWORD*)(MaxCount+0x00);
			//printf("MC:%08X\n",MaxCount);
			if(!MaxCount) return NULL;
			//printf("MC:%08X\n",MaxCount+0x40);
			MaxCount = *(DWORD*)(MaxCount+0x40);
			//printf("MC:%08X\n",MaxCount);
			while (counter <= MaxCount)
			{
				++counter;
				//if(counter == 1) printf("EP:%08X\n",pObjectMgr);
				uielemePointer = *(DWORD*)pObjectMgr;
				//if(counter == 1) printf("EP:%08X\n",uielemePointer);
				if(!uielemePointer) return NULL;
				//if(counter == 1) printf("EP:%08X\n",uielemePointer+0x974);
				uielemePointer = *(DWORD*)(uielemePointer+0x974);
				//if(counter == 1) printf("EP:%08X\n",uielemePointer);
				if(!uielemePointer) return NULL;
				//if(counter == 1) printf("EP:%08X\n",uielemePointer+0x00);
				uielemePointer = *(DWORD*)(uielemePointer+0x00);
				//if(counter == 1) printf("EP:%08X\n",uielemePointer);
				if(!uielemePointer) return NULL;
				//if(counter == 1) printf("EP:%08X\n",uielemePointer+0x08);
				uielemePointer = *(DWORD*)(uielemePointer+0x08);
				//if(counter == 1) printf("EP:%08X\n",uielemePointer);
				if(!uielemePointer) return NULL;
				//if(counter == 1) printf("EP:%08X\n",uielemePointer+(counter*4));
				uielemePointer = uielemePointer+(counter*4);
				//if(counter == 1) printf("EP:%08X\n",uielemePointer);
				while (uielemePointer != NULL)
				{
					if((nPnt = *(DWORD*)uielemePointer) != NULL && (nPnt = *(DWORD*)(nPnt + 0x210)) != NULL)
					{
						if(!strcmp(((UIElementStruct*)nPnt)->name,Label))
						{
							return (UIElementStruct*)nPnt;
						}
					}
					uielemePointer = *(DWORD*)uielemePointer;
				}
			}
			return NULL;
		}
		__except(1)
		{
			return NULL;
		}
	}
	static boolean inStr(const char* Text,const char *search)
	{
		int StringLenSearch = strlen(search);
		if(!StringLenSearch) return false;
		int StringLenText = strlen(Text);

		for(int i=0; i < StringLenText - StringLenSearch + 1;++i)
		{
			if(!memcmp((Text+i), search, StringLenSearch))
				return true;
		}
		return false;
	}
	static int SetText(const char *Label, const char* Text)
	{
		//printf("set %s:%s\n",Label,Text);
		UIElementStruct* CurrElement = NULL;
		if((CurrElement = GetUI(Label)) != NULL)
		{
			//printf("good\n");
			strcpy(CurrElement->pntrText,Text);
			return 1;
		}
		//printf("bad\n");
		return -1;
	}
	static int Login(const char* Email,const char* Password)
	{ 
		//printf("1\n");
		if(UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog") || UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog.HeroListChecked") || UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog.LogonChecked") || UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog.ConnectStarted") || UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog.CancelLogonButton"))	return 2;
		//printf("2\n");
		if(UIElements::GetVisibility("Root.TopLayer.BattleNetModalNotifications_main.ModalNotification.Buttons.ButtonList.OkButton")) return 3;
		//printf("3\n");
		if(UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.ChangeQuestButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.PublicButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.AuctionHouseButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.PlayGameButton")) return 1;
		//printf("4\n");
		if(UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.LoginContainer.AccountInput") ){//&& !UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog.CancelLogonButton") && !UIElements::GetVisibility("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.StatusDialog")){
		//printf("5\n");
		int LoggedInValuesSet = SetText("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.LoginContainer.AccountInput",Email) && SetText("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.LoginContainer.PasswordInput",Password);
		if(LoggedInValuesSet == 1)
		{
				//printf("call login...\n");
				((DefLogin)pDefLogin)();
				//ClickUI("Root.NormalLayer.BattleNetLogin_main.LayoutRoot.LoginContainer.SubmitButton");
		}
		}
		if(UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.ChangeQuestButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.PublicButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.AuctionHouseButton") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.PlayGameButton")) return 1;
		return 0;
	}
	static int GetVisibility(const char *Label)
	{
		UIElementStruct* CurrElement = NULL;
		if((CurrElement = GetUI(Label)) != NULL)
		{
			//printf("%s:%d\n",Label,CurrElement->visible);
			return CurrElement->visible;
		}
		return 0;
	}
	static int ClickUI(const char * Label)
	{
		typedef void ( __thiscall * UIClick)(int UIElement);
		if(GetVisibility(Label) != NULL)
		{
			((UIClick)UIElements::GetUI(Label)->byte_1[261])((int)UIElements::GetUI(Label));
			return 1;
		}
		return 0;
	}
			
};

class ACDActors 
{
	public:
		enum UnitType{Invalid, Monster, Gizmo, Client_Effect, Server_Prop, Environment, Critter, Player, Item, Axe_Symbol, Projectile, Custom_Brain};	
		enum ItemLocation{
			UNK = -1, Inventory = 0, Head = 1, Chest = 2, OffHand = 3, Mainhand = 4, Hands = 5, Belt = 6, Boots = 7, Shoulders = 8, Legs = 9,
			Wrists = 10, LeftRing = 11, RightRing = 12, Neck = 13, ItemsForBuyBack = 14, Stash = 15, Gold = 16, ItemForSale = 17, Merchant = 19, 
			FollowerRightHand = 22, FollowerLeftHand = 23, FollowerSpecial = 24, FollowerNeck = 25, FollowerRightFinger = 26, FollowerLeftFinger = 27
		};
		struct ACDActor
		{ 	
			ULONG id_acd;     // 0x000 
			CHAR Name[128];     // 0x004 
			ULONG unk_0;     // 0x084 
			ULONG id_Identify;     // 0x088 
			ULONG id_GUID;     // 0x08C 
			ULONG id_sno;     // 0x090 
			UCHAR unknown_94[32];     // 0x094 
			ULONG id_acd_gBall;     // 0x0B4 
			UCHAR unknown_B8[24];     // 0x0B8 
			float X;
			float Y;
			float Z;
			UCHAR unknown_DC[36];     // 0x0DC 
			float RadiusDefault;     // 0x100 
			UCHAR unknown_104[4];     // 0x104 
			ULONG id_world;     // 0x108 
			UCHAR unknown_10C[4];     // 0x10C 
			ULONG id_owner;     // 0x110 
			ItemLocation ItemLocation; // 0= Backpack 15= stash
			DWORD ItemPosX; 
			DWORD ItemPosY; 
			UCHAR unknown_114[9];     // 0x114 
			ULONG id_attrib;     // 0x120 
			ULONG id_unk3;     // 0x124 
			UCHAR unknown_128[244];     // 0x128 
			UCHAR NBAD848;     // 0x21C 
			UCHAR RadiusType;     // 0x21D 
			UCHAR NBB56E9;     // 0x21E 
			UCHAR NBBA038;     // 0x21F 
			UCHAR unknown_220[24];     // 0x220 
			float RadiusScaled;     // 0x238 
			UCHAR unknown_23C[148];     // 0x23C 
			//ACD+0x164. is identified?
		};
		static boolean inStr(const char* Text,const char *search)
		{
			if(!strlen(search)) return false;
			if(strstr(Text,search) > 0)	return true;
			return false;
		}
		static DWORD GetGroupListPtrByName(const char *Name)
		{	
			int *SnoList = (int*)(*(DWORD*)(pGetSnoGroupByIndex+0x13));
			for(int i=0;i<57;++i)
			{
				if(!SnoList[2*i]) continue;
				if(!strcmp(Name, (char*)(*(DWORD*)SnoList[2*i] + 0x1C)))
					return *(DWORD*)SnoList[2*i];
			}
			return 0;
		}
		static int isGizmo(ACDActors::ACDActor *ACD_Ptr)
		{
			typedef int ( __thiscall * IsGizmo)(ACDActors::ACDActor * ACDPtr);		
			//return GetUnitTypeFromSnoID(ACD_Ptr, "Actor") == Gizmo;
			return ((IsGizmo)pIsGizmo)(ACD_Ptr);

		}
		static int isMonster(ACDActors::ACDActor *ACD_Ptr)
		{
			typedef int ( __thiscall * IsMonster)(ACDActors::ACDActor * ACDPtr);	
			//return GetUnitTypeFromSnoID(ACD_Ptr, "Actor") == Monster;
			return ((IsMonster)pIsMonster)(ACD_Ptr);
		}
		static int isItem(ACDActors::ACDActor *ACD_Ptr)
		{
			//return 1;
			//typedef int ( __thiscall * IsMonster)(ACDActors::ACDActor * ACDPtr);	
			return (GetUnitTypeFromSnoID(ACD_Ptr, "Actor") == Item);
		}
		static int isChest(ACDActors::ACDActor *ACD_Ptr)
		{
			if(!ACDActors::isGizmo(ACD_Ptr) || 
				GetAttributByString(ACD_Ptr,"Gizmo_Has_Been_Operated") || GetAttributByString(ACD_Ptr,"Chest_Open") ||
				((!ACDActors::inStr(ACD_Ptr->Name,"_Chest") || !ACDActors::inStr(ACD_Ptr->Name,"_chest")) && !ACDActors::inStr(ACD_Ptr->Name,"_Container"))) return 0;
			
			switch(ACD_Ptr->id_sno)
			{
				case 0xFB0F/*trOut_Stump_Chest */:
					return 0;
			}

			return 1;	
		}
		static int GetUnitTypeFromSnoID(ACDActors::ACDActor *ACD_Ptr, const char* List)
		{
			typedef int ( __thiscall * GetSNOInfo)(int SNOList, int SNO_ID, int zero);
			typedef int ( __thiscall * DecreaseRefCount)(int SNOList, DWORD* GetSNOInfoRetPtr);
			DWORD ScenePtr = GetGroupListPtrByName(List);
			if(!ScenePtr || !ACD_Ptr) return 0;
			DWORD InfoStructPtr = ((GetSNOInfo)pGetSNOInfo)(ScenePtr,ACD_Ptr->id_sno,0);
			if(!InfoStructPtr) return 0;
			DWORD UnitType = *(DWORD*)(InfoStructPtr+0x10);
			((DecreaseRefCount)pDecreaseRefCount)(ScenePtr,&InfoStructPtr);
			return UnitType;
		}
		static ACDActor * GetACDActorByGUID(ULONG GUID)
		{
			__try
			{
				if(GUID == -1) return NULL;
				for(int i=0;i<= GetMaxACDActor();++i)
				{
					if(GetACDActor(i) == NULL || GUID != GetACDActor(i)->id_GUID) continue;
					return GetACDActor(i);
				}
				return NULL;
			} __except(1){return NULL;}
		}
		static ACDActor * GetNearestACDActorByName(char *Name)
		{
			typedef int ( __cdecl * GetPlayerGUID)();
			__try
			{
				int Found = 1;
				float SaveDis = 999999;
				float CurrDis = 0;
				int FoundActorID = -1;
				ACDActor* ACDME = GetACDActorByGUID(((GetPlayerGUID)pGetPlayerGUID)());
				if(ACDME == NULL) return NULL;
				for(int i=0;i<= GetMaxACDActor();++i)
				{
					if(GetACDActor(i) != NULL)
					{
						if(GetACDActor(i)->id_GUID == -1) continue;
						Found = 0;
						//if(strstr(GetACDActor(i)->Name,"A1_UniqueVendor_Miner_InTown_03")) printf("%s\n",GetACDActor(i)->Name);
							if(strstr(GetACDActor(i)->Name,Name))
							{
								Found = 1;
							}
						if(Found == 1)
						{
							CurrDis = sqrt(pow(ACDME->X - GetACDActor(i)->X,2)+pow(ACDME->Y-GetACDActor(i)->Y,2));
							if(CurrDis < SaveDis)
							{
								SaveDis = CurrDis;
								FoundActorID = i;
								break;
							}
						}
					}
				}
				if(FoundActorID != -1)
					return GetACDActor(FoundActorID);
				return NULL;
			} __except(1){return NULL;}
		}
		static ACDActor * GetNearestActorByModelID(ULONG ModelID)
		{
			typedef int ( __cdecl * GetPlayerGUID)();
			__try
			{
				float SaveDis = 9999.99f;
				float CurrDis = 0.0f;
				int SaveActorNum = -1;
				int InteractedGizmoID = -1;
				ACDActor* ACDME = GetACDActorByGUID(((GetPlayerGUID)pGetPlayerGUID)());
				if(ACDME == NULL) return NULL;
				for(int i=0;i <= GetMaxACDActor();++i)
				{
					if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && ACDME->id_GUID != GetACDActor(i)->id_GUID)
					{
						CurrDis = sqrt(pow(ACDME->X - GetACDActor(i)->X,2)+pow(ACDME->Y-GetACDActor(i)->Y,2));
						if(GetACDActor(i)->id_sno == ModelID || ModelID == 0)
						{
							if(CurrDis < SaveDis)
							{
								SaveDis = CurrDis;
								SaveActorNum = i;
							}
						}
					}
				}
				if(InteractedGizmoID != -1)
					SaveActorNum = InteractedGizmoID;
				if(SaveActorNum != -1)
				{
					return GetACDActor(SaveActorNum);
				}
				return NULL;
			} __except(1){return NULL;}
		}
		static DWORD GetRActorPtrFromGUID(int GUID)
		{
			typedef int ( __cdecl * GetActorPtrFromGUIDWrapper)(int GUID);
			return ((GetActorPtrFromGUIDWrapper)pGetActorPtrFromGUIDWrapper)(GUID);
		}
		static ULONG GetMaxACDActor()
		{
			__try 
			{
				DWORD Ptr = *(DWORD*)pObjectMgr;
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x8A0);
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x00);
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x108);
				return Ptr;
			}__except(1) {
				return NULL;
			}
		}
		static ULONG CanSellItem(ACDActor *Item)
		{
			typedef signed int ( __cdecl * CanSellItem)(ACDActor * ACDPtr);
			if(Item == NULL || !isItem(Item) || Item->ItemLocation != Inventory) return -1;
			return ((CanSellItem)pCanSellItem)(Item);
		}
		static ULONG CanUseItem(ACDActor *Item)
		{
			typedef bool ( __cdecl * CanUseItem)(int a1, int a2); // E41380
			if(Item == NULL || !isItem(Item) || Item->ItemLocation != Inventory) return -1;
			return ((CanUseItem)pCanUseItem)(1,2);;
		}
		static ULONG UseItem(ACDActor *Item)
		{
			typedef int ( __cdecl * UseItem)(ACDActor * ACDPtr, int NegativOne, int ONE); //0x00A69240
			if(!isItem(Item) || Item->ItemLocation != Inventory) return -1;
			return ((UseItem)pUseItem)(Item,-1,1);
		}
		static ACDActor * GetACDActor(unsigned int aIndex)
		{
			if(aIndex > GetMaxACDActor()){
				return NULL;
			}
			__try 
			{
				DWORD Ptr = *(DWORD*)pObjectMgr;
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x8A0);
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x00);
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x148);
				if(!Ptr) return NULL;
				Ptr = *(DWORD*)(Ptr+0x00);
				return (ACDActor*)(Ptr+(aIndex*0x2D0));
			}__except(1) {
				return NULL;
			}
		}	
		static float GetAttributByString(ACDActor* ACDPtr, char* Attrib)
		{
			typedef int (__thiscall *GetInt)(int ACDPtr, int AttributeKey);
			typedef double (__thiscall *GetDouble)(int ACDPtr, int AttributeKey);
			__try
			{
				if(ACDPtr != NULL)
				{
					for(int i=0;i<=0x036E;++i)
					{
						if(!strcmp(ACDActors::GetAttributeDesc(i).Name, Attrib))
						{
							if(ACDActors::GetAttributeDesc(i).Type == 0)
							{
								return ((GetDouble)pGetDouble)((int)ACDPtr,((int)0xFFFFF000 | ACDActors::GetAttributeDesc(i).id));
							}
							else if(ACDActors::GetAttributeDesc(i).Type == 1)
							{
								return (float)((GetInt)pGetInt)((int)ACDPtr,((int)0xFFFFF000 | ACDActors::GetAttributeDesc(i).id));
							}
						}
					}
				}
				return -1;
			}__except(1){return 0;}
		}
		struct AttributeDesc 
		{ 
			/* 0x00 */ int id; 
			/* 0x04 */ int DefaultVal; // for when trying to get an attribute that doesn't exist in a FastAttributeGroup 
			/* 0x08 */ int unk2; 
			/* 0x0C */ int unk3; 
			/* 0x10 */ int Type; // 0 = float, 1 = int 
			/* 0x14 */ char* Formula1; 
			/* 0x18 */ char* Formula2; 
			/* 0x1C */ char* Name; 
			/* 0x20 */ void* unk5; 
			/* 0x24 */ int unk6; 
		}; 
		static AttributeDesc GetAttributeDesc(unsigned int aIndex){
			return ((AttributeDesc*)pAttributeDescriptionList)[aIndex];
		}
};

	struct spellrule{
		int skillid;
		int priority;
		int recast;
		int min_resource;
		int max_resource;
		int min_hp;
		int max_hp;
		int speed_increase;
		int monster_count;
		int monster_range;
		int cast_range;
		int min_resource2;
		int max_resource2;
	};

class MyACD : ACDActors
{
public:
	static int countSpellRules(){
		int cnt=0;
		char *pch;
		pch = strstr(spellrulesbuffer,"\n");
		if(pch!=NULL) {
	        cnt++;
		    while((pch = (strstr(pch,"\n")+2)) && (((int)pch-(int)&spellrulesbuffer))<=strlen(spellrulesbuffer)) cnt++;
		}
		return cnt;
	}
	static spellrule* loadSpellRules(char *spellrulepath,int spellrulesenabled){
		static spellrule* ruleArray;
		//printf("enabled:%d\n",spellrulesenabled);
		if(spellrulesenabled == 1){
			//printf("loading...\n");
			char *line;
			int rulenumber = 0;
			int SpellRuleFileSize = getFileSize(spellrulepath);
			char *buffer;
			FILE * pFile;
			size_t result;
			pFile = fopen (spellrulepath, "r" );
			buffer = (char*) malloc (sizeof(char)*SpellRuleFileSize+1);
			result = fread(buffer,1,SpellRuleFileSize,pFile);
			buffer[result] = 0x00;
			strncpy(spellrulesbuffer,buffer,(result+1));
			fclose (pFile);
			free(buffer);
			ruleArray = new spellrule[countSpellRules()];
			//printf("%s\n",spellrulesbuffer);
			line = strtok(spellrulesbuffer,"\n");
			//printf("fline:%s\n",line);
			while (line != NULL)
			{
				if(!sscanf(line,"%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d",&ruleArray[rulenumber].skillid,&ruleArray[rulenumber].priority,&ruleArray[rulenumber].recast,&ruleArray[rulenumber].min_resource,&ruleArray[rulenumber].max_resource,&ruleArray[rulenumber].min_hp,&ruleArray[rulenumber].max_hp,&ruleArray[rulenumber].speed_increase,&ruleArray[rulenumber].monster_count,&ruleArray[rulenumber].monster_range,&ruleArray[rulenumber].cast_range,&ruleArray[rulenumber].min_resource2,&ruleArray[rulenumber].max_resource2)) return NULL;
				//printf("%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d,%d\n",ruleArray[rulenumber].skillid,ruleArray[rulenumber].priority,ruleArray[rulenumber].recast,ruleArray[rulenumber].min_resource,ruleArray[rulenumber].max_resource,ruleArray[rulenumber].min_hp,ruleArray[rulenumber].max_hp,ruleArray[rulenumber].speed_increase,ruleArray[rulenumber].monster_count,ruleArray[rulenumber].monster_range,ruleArray[rulenumber].cast_range,ruleArray[rulenumber].min_resource2,ruleArray[rulenumber].max_resource2);
				line = strtok(NULL, "\n");
				++rulenumber;
			}
		} else {
			printf("returning disabled....\n");
			ruleArray = NULL;
		}	
			return ruleArray;
	}
	static int GetSkillCount(){
		int cnt = 0;
		for(int i = 0; i<6; ++i){
			if(GetSkillID(i) > 0) ++cnt;
		}
	return cnt;
	}
	static void GetSkillPriorityList(int *id_array, int s, char *spellrulepath, int spellrulesenabled){
		spellrule* ruleArray = loadSpellRules(spellrulepath,spellrulesenabled);
		static int donelist[6];
		static int loaded;
		if(loaded <=0 || spellrulesenabled == 0){
			//printf("loading2....\n");
			if(ruleArray != NULL){
				//printf("from string....\n");
				for(int i=0; i<s; ++i){
					int lowest_priority=100;
					int lowest_index = -1;
					for(int a=0;a<s;++a){
						int used = 0;
						for(int g=0; g<s;++g){
							if(id_array[g] == ruleArray[a].skillid) used = 1;
						}
						if(ruleArray[a].priority < lowest_priority && used != 1){
							lowest_index = a;
							lowest_priority = ruleArray[a].priority;
						}
					}
            
					id_array[i] = ruleArray[lowest_index].skillid;
					donelist[i] = ruleArray[lowest_index].skillid;
				}
			} else {
				for(int i=0; i<s; ++i){
					int sId = GetSkillID(i);
					if(sId <= 0) continue;
					id_array[i] = sId > 0 ? sId : 0;
					donelist[i] = sId > 0 ? sId : 0;
				}
			}
			loaded = 1;
		} else {
			//printf("reloading....\n");
			for(int h=0;h<s;++h){
				id_array[h] = donelist[h];
			}
		}
		return;
	}

	static spellrule* GetSpellRulesForId(int skillId,int cnt, char *spellrulepath,int spellrulesenabled){
		spellrule* ruleArray = loadSpellRules(spellrulepath,spellrulesenabled);
		if(ruleArray != NULL){
			for(int i =0; i<cnt; ++i){
				//printf("rule for:%d\n",ruleArray[i]);
				if(ruleArray[i].skillid == skillId) return &ruleArray[i];
			}
		}
		return NULL;
	}
	static ACDActor * GetMyACD()
	{
		typedef int ( __cdecl * GetPlayerGUID)();
		DWORD GUID = ((GetPlayerGUID)pGetPlayerGUID)();
		if(GUID == -1) return NULL;
		return GetACDActorByGUID(GUID);
	}
	static bool checkSpellRules(int SkillID,int HP_percent,int resource,int resource2, int cnt, char *spellrulepath, int spellrulesenabled, bool moving = false){
		spellrule* skillRules = GetSpellRulesForId(SkillID,cnt,spellrulepath,spellrulesenabled);
		//printf("\nid:%d-1 ",SkillID);
		if(skillRules != NULL){
			skillRules->max_resource = skillRules->max_resource <= 0 ? resource : skillRules->max_resource;
			skillRules->max_resource2 = skillRules->max_resource2 <= 0 ? resource2 : skillRules->max_resource2;
			skillRules->max_hp = skillRules->max_hp <= 0 ? 100 : skillRules->max_hp;
			skillRules->cast_range = skillRules->cast_range <= 0 ? 20 : skillRules->cast_range;
			//printf("2 ");
			if(SkillAktive(SkillID) && skillRules->recast != 1) return false;
			//printf("3 ");
			if(HP_percent > skillRules->max_hp  || HP_percent < skillRules->min_hp) return false;
			//printf("4 ");
			if(resource > skillRules->max_resource || resource < skillRules->min_resource) return false;
			//printf("5 ");
			if(resource2 > 0 && (resource2 > skillRules->max_resource2 || resource2 < skillRules->min_resource2)) return false;
			//printf("6 ");
			if(getMonsterCountWithin(skillRules->monster_range) < skillRules->monster_count) return false;
			//printf("7 ");
			if(getMonsterCountWithin(skillRules->cast_range) < 1) return false;
			//printf("8 ");
			if(!IsSkillReady(SkillID)) return false;
			//printf("9 ");
			if(skillRules->speed_increase == 1 && moving == true) return true;
			//printf("10 ");
		}
		if(SkillID <= 0 || !IsSkillReady(SkillID) || moving) return false;
		//printf("11\n");
		return true;
	}

	static int UsePotion(bool onlyCheck = false)
	{
		ACDActor * ACDPotion = NULL;
		ACDActor * Minor = NULL,
					*Lesser = NULL,
					*Normal = NULL,
					*Greater = NULL,
					*Large = NULL,
					*Super = NULL,
					*Heroic = NULL,
					*Resplendent = NULL,
					*Runic = NULL,
					*Mythic = NULL;
		for(int i=0;i<=ACDActors::GetMaxACDActor();++i)
		{
			if(GetACDActor(i) == NULL) continue;
			__try
			{
				if(ACDActors::GetACDActor(i) != NULL && isItem(ACDActors::GetACDActor(i)) && ACDActors::GetACDActor(i)->ItemLocation == ACDActors::ItemLocation::Inventory)
				{
					switch(GetACDActor(i)->id_sno)
					{
						case 4440: // healthPotion_Minor
							Minor = GetACDActor(i);
							break;
						case 4439: // healthPotion_Lesser
							Lesser = GetACDActor(i);
							break;
						case 4441: // healthPotion_Normal
							Normal = GetACDActor(i);
							break;
						case 4438: // healthPotion_Greater
							Greater = GetACDActor(i);
							break;
						case 4436: // HealthPotionLarge (Major?)
							Large = GetACDActor(i);
							break;
						case 4442: // healthPotion_Super
							Super = GetACDActor(i);
							break;
						case 226395: // healthPotion_Heroic
							Heroic = GetACDActor(i);
							break;
						case 226396: // healthPotion_Resplendent
							Resplendent = GetACDActor(i);
							break;
						case 226398: // healthPotion_Runic
							Runic = GetACDActor(i);
							break;
						case 226397: // healthPotion_Mythic
							Mythic = GetACDActor(i);
							break;
						default:
							break;
					}
				}
			}__except(1){}
		}
		if(Mythic) ACDPotion = Mythic;
		else if(Runic) ACDPotion = Runic;

		else if(Resplendent) ACDPotion = Resplendent;
		else if(Heroic) ACDPotion = Heroic;
		else if(Super) ACDPotion = Super;
		else if(Large) ACDPotion = Large;
		else if(Greater) ACDPotion = Greater;
		else if(Normal) ACDPotion = Normal;
		else if(Lesser) ACDPotion = Lesser;
		else if(Minor) ACDPotion = Minor;
		if(ACDPotion)
		{
			if(!onlyCheck)
				MyACD::UseItem(ACDPotion);
			return 1;
		}
		else
		{
			return 0;
		}
	}
	static int GetSkillID(int INDEX)
	{
		__try{
			if(INDEX < 0 || INDEX > 6) 
				return -1;
			DWORD SkillPtr = (DWORD)pObjectMgr;
			if(!SkillPtr) return NULL;
			SkillPtr = *(DWORD*)SkillPtr;
			if(!SkillPtr) return NULL;
			SkillPtr = *(DWORD*)(SkillPtr + 0x874);
			if(!SkillPtr) return NULL;
			SkillPtr = *(DWORD*)(SkillPtr + 0x60 + 0x9D0 + (INDEX * 8));
			if(!SkillPtr) return NULL;
			return SkillPtr;
		}
		__except(1)
		{
			return -1;
		}
	}
	static int BackbackFreeDoubleSlot() 
	{
		typedef signed int ( __cdecl * ItemPosition)(void* Item);
		typedef struct  {
			unsigned long PlayerACDID;
			unsigned long _location;
			unsigned long _position_x;
			unsigned long _position_y;
		}ItemPosition_PACKET;
		ItemPosition_PACKET Item,iY;
		Item.PlayerACDID = iY.PlayerACDID = GetMyACD()->id_acd;
		Item._location = iY._location = 0;
		for(Item._position_x = iY._position_x = 0;Item._position_x<10;++Item._position_x,++iY._position_x)
		{
			for(Item._position_y=0;Item._position_y<6;++Item._position_y)
			{
				iY._position_y = Item._position_y + ((Item._position_y == 5)?0:1);
				if((((ItemPosition)pItemPosition)(&Item) == -1) && (Item._position_y != 5 && ((ItemPosition)pItemPosition)(&iY) == -1))
					return 1;
			}
		}
		return 0;
	}
	static int getFileSize(char* path){
		FILE *fp = fopen(path,"r");
		if (fp==NULL) {
			//printf("failed to open file\n");
			return -1;
		}
        fseek(fp,0,SEEK_END);
        int size = ftell(fp);
        fclose(fp);
        return size;
	}
	static bool LoadLootTable(){
		char lootTablePath[MAX_PATH];
		strcpy(lootTablePath,RespawnPath);
		strcat(lootTablePath,"\\LootTable.txt");
		int CurrentLootTableFileSize = getFileSize(lootTablePath);
		if((strlen(lootTableBuffer) <=0 || lootTableSize != CurrentLootTableFileSize) && CurrentLootTableFileSize != -1){
			//load loot table
			//printf("loading loottable\n");
			char *buffer;
			size_t result;
			FILE *LootTableFile = fopen(lootTablePath,"r");
			buffer = (char*) malloc (sizeof(char)*CurrentLootTableFileSize+1);
			if (buffer == NULL) {
				//printf("memory error\n");
				return false;
			}
			result = fread(buffer,1,CurrentLootTableFileSize,LootTableFile);
			buffer[result] = 0x00;
			strncpy(lootTableBuffer,buffer,CurrentLootTableFileSize);
			fclose (LootTableFile);
			free(buffer);
			lootTableSize = CurrentLootTableFileSize;
			return true;
		}
		return strlen(lootTableBuffer) > 0;
	}
	static signed int BackpackFreeSlots()
	{
			typedef signed int ( __cdecl * ItemPosition)(void* Item);
			typedef struct  {
				unsigned long PlayerACDID;
				unsigned long _location;
				unsigned long _position_x;
				unsigned long _position_y;
			}ItemPosition_PACKET;
			ItemPosition_PACKET Item;
			int Free_Slots = 0;
			Item.PlayerACDID = GetMyACD()->id_acd;
			Item._location = 0;
			for(Item._position_x = 0;Item._position_x<10;++Item._position_x)
			{
				for(Item._position_y=0;Item._position_y<6;++Item._position_y)
				{
					if(((ItemPosition)pItemPosition)(&Item) == -1)
						++Free_Slots;
				}
			}
		return Free_Slots;
	}
	static int IdentItem(ACDActors::ACDActor* ACDPtr)
	{
		if(!isItem(ACDPtr)) return 0;

		typedef void (__cdecl *IdentifyItem)(unsigned long _0x12A, void*, unsigned long _0x0C);
		typedef struct 
		{
			unsigned long _ulBC;
			unsigned long _ulBD;
			unsigned long _item_id;
		}ItemIdent_PACKET;
		ItemIdent_PACKET Item;
		Item._item_id = ACDPtr->id_Identify;
		//printf("ID:%08X\n",ACDPtr->id_Identify);
		Item._ulBC = 0xBC;
		Item._ulBD = 0xBD;
		((IdentifyItem)pIdentifyItem)(0x12A,&Item,0x0c);
		return 1;
	}
	static float DurabilityPercent()
	{
		float Cur = 0;
		float Max = 0;
		for(int i=0;i<=ACDActors::GetMaxACDActor();++i)
		{
			__try
			{
				if(ACDActors::GetACDActor(i) != NULL && isItem(ACDActors::GetACDActor(i)) && ACDActors::GetACDActor(i)->ItemLocation >= 1 && ACDActors::GetACDActor(i)->ItemLocation <= 13)
				{
					Cur += ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Durability_Cur");
					Max += ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Durability_Max");
				}
			}__except(1){}
		}
		return (float)(Cur/Max*100);
	}
	static float GetGFRadius()
	{
		float radius = 0;
		for(int i=0;i<=ACDActors::GetMaxACDActor();++i)
		{
			__try
			{
				if(ACDActors::GetACDActor(i) == NULL || !isItem(ACDActors::GetACDActor(i)) || ACDActors::GetACDActor(i)->ItemLocation < 1 || ACDActors::GetACDActor(i)->ItemLocation > 13) continue;
				radius += ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Gold_PickUp_Radius");
			}__except(1){return 0;}
		}
		return radius;
	}
	static float GetGF()
	{
		float radius = 0;
		for(int i=0;i<=ACDActors::GetMaxACDActor();++i)
		{
			__try
			{
				if(ACDActors::GetACDActor(i) == NULL || !isItem(ACDActors::GetACDActor(i)) || ACDActors::GetACDActor(i)->ItemLocation < 1 || ACDActors::GetACDActor(i)->ItemLocation > 13) continue;
				radius += (ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Gold_Find")*100);
			}__except(1){return 0;}
		}
		return radius;
	}
	static void SkipScene()
	{
		typedef int ( __cdecl * SkipScene)();
		if(MyACD::GetMyACD() != NULL){
			((SkipScene)pSkipScene)();
		}
	}
	static float getResources(int b=1, int a=0x86)
		{
			float value;
				ACDActors::ACDActor *me = GetMyACD();
				__asm pushad
				__asm mov esi,b
				__asm mov ecx,me
				__asm push b
				__asm push a
				__asm call pGetResources
				__asm FST DWORD PTR SS:[value]
				__asm popad
			return value;
		}
	static int TakeScreenShot()
	{
		typedef int ( __cdecl *ScreenShot)(void);
		return (int)(strstr(((char*)((DWORD*)*((DWORD*)((ScreenShot)pScreenShot)())) + 0x14),".jpg")-0xD);
	}
	static int GetPlayerClass()
	{
		typedef int ( __cdecl *GetClass)(ACDActors::ACDActor*, int);
		ACDActor* ACDPtr = GetMyACD();
		int pclass;
			if(ACDPtr != NULL)
			{
				pclass = ((GetClass)pGetClass)(GetMyACD(),1);
				return pclass;
			}
		return -1;
	}
	static void Revival()
	{
		typedef int ( __cdecl * Revival)();
		((Revival)pRevival)();
	}
	static void Repair(int Flag=1)
	{
		typedef int ( __cdecl * Repair)(int Flag/*0=all || 1=alles angelegte*/);
		((Repair)pRepair)(Flag);
	}
	static void SellItem(ACDActors::ACDActor* Item)
	{
		typedef int ( __cdecl * SellItem)(ACDActors::ACDActor* ACDPtr);
		((SellItem)pSellItem)(Item);
	}
	/*static DWORD* GetWeaponDPS(ACDActors::ACDActor* Item)
	{
		DWORD addr;
		typedef int ( __cdecl *GetWeaponDPS)(DWORD*,ACDActors::ACDActor*,int);
		((GetWeaponDPS)pGetWeaponDPS)(&addr,Item,-1);
		return &addr;
	}*/
	static void LeaveWorld()
	{
		typedef int ( __cdecl * LeaveWorld)(int UNK);
		((LeaveWorld)pLeaveWorld)(7);
	}
	static void EnterWorld()
	{
		typedef int ( __cdecl * EnterWorld)(int UNK);
		((EnterWorld)pEnterWorld)(7);
	}
	static int SkillAktive(int SkillID) 
	{ 
		typedef int (__thiscall *GetInt)(int ACDPtr, int AttributeKey);
		__try
		{
			ACDActor* ACDPtr = GetMyACD();
			if(ACDPtr != NULL)
			{
				for(int i=0;i<=0x339;++i)
				{
					if(!strcmp(GetAttributeDesc(i).Name,"Buff_Active"))
					{
						return ((GetInt)pGetInt)((int)ACDPtr,( SkillID << 12 ) | GetAttributeDesc(i).id);
					}
				}
			}
			return -1;
		}__except(1){return -1;}
	} 
	static int IsSkillReady(int SkillID) 
	{ 
		typedef int ( __cdecl * IsSkillReady)(int ACDPtr, int SkillID, int zero);
		return (((IsSkillReady)pIsSkillReady)((int)GetMyACD(), SkillID, 0) == 0);
	} 
	static int GetGold() 
	{ 
		typedef int ( __thiscall * GetGold)(int MyACD);
		return ((GetGold)pGetGold)((int)GetMyACD());
	} 
	static void useTownportal() 
	{ 
		typedef int ( __cdecl * UseTownPortal)();
		((UseTownPortal)pUseTownPortal)();
	}
	static int GetQuest(int *Act, int *QuestID, int *QuestStep, int *SubQuestID) 
	{ 
		__try
		{
			typedef int ( __cdecl * GetQuestPtr)();	
			typedef int ( __cdecl * GetAct)();

			*Act = ((GetAct)pGetAct)();
			DWORD QuestStructPtr = ((GetQuestPtr)pGetQuestPtr)();
			if(QuestStructPtr)
			{
				*QuestID = *(DWORD*)QuestStructPtr;
				*QuestStep = *((DWORD*)(QuestStructPtr+0x18));
				*SubQuestID =*(DWORD*)(QuestStructPtr+0x1C);
			}
		}
		__except(1){return NULL;}
		return 1;
	} 
	static int IdentifyItems()
	{
		typedef int ( __cdecl *GetItemValue)(ACDActors::ACDActor *MyACD, int a, ACDActors::ACDActor *ACDPtr, int b);
		try
		{
			if(MyACD::SkillAktive(0x375C5/*IdentifyWithCast*/)==1)
			{
				return 1;
			}
			for(int i=0;i<= ACDActors::GetMaxACDActor();++i)
			{
				if((((GetItemValue)pItemValue)(GetACDActor(i),-1,MyACD::GetMyACD(),-1) == 0 || ((GetItemValue)pItemValue)(GetACDActor(i),0,MyACD::GetMyACD(),0) == 0) && ACDActors::GetACDActor(i) != NULL && isItem(ACDActors::GetACDActor(i)) && ACDActors::GetACDActor(i)->ItemLocation == ACDActors::Inventory /*&& ACDActors::CanSellItem(ACDActors::GetACDActor(i))*/)
				{
					//printf("about to ident\n");
					MyACD::IdentItem(ACDActors::GetACDActor(i));
					return 1;
				}
			}
			return 0;
		}
		catch(...)
		{
			//printf("Error while identifying");
			return 0;
		}
	}
	static ACDActor * GetNearestShrine(int BlessedShrine, int EnlightenedShrine, int FortuneShrine, int FrenziedShrine, int EmpoweredShrine, int FleetingShrine, int HealingWell, int useHealingWellAt, int curHealthPercent, float Range = 9999.9f)
	{
		__try
		{
			float SaveDis = Range;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && ACDActors::isGizmo(GetACDActor(i)))
				{
					if((BlessedShrine == 1 && GetACDActor(i)->id_sno == 176074) ||  (EnlightenedShrine == 1 && GetACDActor(i)->id_sno == 176075) ||
						(FortuneShrine == 1 && GetACDActor(i)->id_sno == 176076) ||  (FrenziedShrine == 1 && GetACDActor(i)->id_sno == 176077) ||
						(EmpoweredShrine == 1 && GetACDActor(i)->id_sno == 260330) ||  (FleetingShrine == 1 && GetACDActor(i)->id_sno == 260331) ||  
						(HealingWell == 1 && GetACDActor(i)->id_sno == 138989 && useHealingWellAt > curHealthPercent))
					{
						if(GetAttributByString(ACDActors::GetACDActor(i),"Untargetable")) continue;
						if(GetAttributByString(ACDActors::GetACDActor(i),"Gizmo_Has_Been_Operated") == 1) continue;
						if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis && CurrDis <= 35)
						{
							SaveDis = CurrDis;
							SaveActorNum = i;
						}
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}
	static ACDActor * GetNearestChest(int Range = 35)
	{
		__try
		{
			float SaveDis = 9999;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && ACDActors::isChest(GetACDActor(i)) && ACDActors::GetAttributByString(GetACDActor(i),"Gizmo_Has_Been_Operated") == 0)
				{
					
					if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis && CurrDis <= Range)
					{
						SaveDis = CurrDis;
						SaveActorNum = i;
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}
	static ACDActor * GetNearestACDMoney(int MinMoneyAmount = 1)
	{
		__try
		{
			float SaveDis = 99999.99;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1)
				{
					if(GetACDActor(i)->id_sno == 0x178 || 
						GetACDActor(i)->id_sno == 0x33130 ||
						GetACDActor(i)->id_sno == 0x10D7 || 
						GetACDActor(i)->id_sno == 0x10D8 || 
						GetACDActor(i)->id_sno == 0x10D9)
					{
						//if(!ACDActors::GetRActorPtrFromGUID(GetACDActor(i)->id_GUID)) continue;
						if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis && 
							ACDActors::GetAttributByString(GetACDActor(i),"Gold") >= MinMoneyAmount &&
							(MyACD::GetGFRadius()-6) < CurrDis)
						{
							SaveDis = CurrDis;
							SaveActorNum = i;
						}
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		}
		__except(1){return NULL;}
	} 
	static ACDActor *GetACDInLootTable()
	{
			if(!LoadLootTable()) return NULL;
		//printf("buffer:%s\n",bufferToCopy);
		__try
		{
			char *line;
			char buffer[2048];
			strcpy(buffer,lootTableBuffer);
			int item_id;
			float range;
			float SaveDis = 99999.99;
			float CurrDis;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			line = strtok(buffer,"\n");
			//printf("fline:%s\n",line);
			while (line != NULL)
			{
				if((line[0] == '/' && line[1] == '/') || line[0] == 0x0d || line[0] == 0x0a) {
					line = strtok (NULL, "\n");
					continue;
				}
				if(line[1] == 'x'){
    				sscanf(line,"%x\x09%f",&item_id,&range);
                } else {
                    sscanf(line,"%d\x09%f",&item_id,&range);
                }
				//printf("id:%d\nrange:%d\n",item_id,range);
				//printf("item_id:%d\n Range:%f\n",item_id,range);
				for(int i=0;i<= GetMaxACDActor();++i)
				{
					//printf("%d",i);
					if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && isItem(GetACDActor(i)) && GetACDActor(i)->ItemLocation == -1)
					{
							//printf("testing:%d\n",GetACDActor(i)->id_sno);
							if(GetACDActor(i)->id_sno == item_id)
							{
								CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2));
								//printf("item Distance:%f\n",CurrDis);
								if(CurrDis < SaveDis <= range)
								{
									SaveDis = CurrDis;
									SaveActorNum = i;
								}
							}
					}
				}
				//printf("line: %s\n",line);
				line = strtok(NULL, "\n");
			}
			if(SaveActorNum != -1) return GetACDActor(SaveActorNum);
			return NULL;
		}
		__except(1){return NULL;}
	}

	static ACDActor * GetNearestACDPotion(int PotionLevel, float Range = 9999.9f)
	{
		if(PotionLevel <= 0) return NULL;
		__try
		{
			float SaveDis = Range;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && isItem(GetACDActor(i)) && GetACDActor(i)->ItemLocation == -1)
				{				
					switch(GetACDActor(i)->id_sno)
					{
						case 4440: // healthPotion_Minor
							if(PotionLevel > 1) break;
						case 4439: // healthPotion_Lesser
							if(PotionLevel > 2) break;
						case 4441: // healthPotion_Normal
							if(PotionLevel > 3) break;
						case 4438: // healthPotion_Greater
							if(PotionLevel > 4) break;
						case 4436: // HealthPotionLarge (Major?)
							if(PotionLevel > 5) break;
						case 4442: // healthPotion_Super
							if(PotionLevel > 6) break;
						case 226395: // healthPotion_Heroic
							if(PotionLevel > 7) break;
						case 226396: // healthPotion_Resplendent
							if(PotionLevel > 8) break;
						case 226398: // healthPotion_Runic
							if(PotionLevel > 9) break;
						case 226397: // healthPotion_Mythic
							if(PotionLevel > 10) break;
						if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis)
						{
							SaveDis = CurrDis;
							SaveActorNum = i;
						}
					default:
						break;
					}

				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}

	static bool is_tome(int item_id){
		int tome_ids[] = {0x2E453,0x38529,0x38528,0xD26};
		for(int i=0; i<(sizeof(tome_ids)/sizeof(int)); ++i){
			if(tome_ids[i] == item_id) return true;
		}
		return false;
	}
	static bool is_page(int item_id){
		int page_ids[] = {0x1AF2A,0x375A3};
		for(int i=0; i<(sizeof(page_ids)/sizeof(int)); ++i){
			if(page_ids[i] == item_id) return true;
		}
		return false;
	}
	static bool checkLootTable(ACDActors::ACDActor *item){
		char item_acd_string[32];
		sprintf(item_acd_string,"%08X\n",item->id_acd_gBall);
		if(strstr(item->Name,"Tome") || strstr(item->Name,"Page") || strstr(itemIdsInLootTable,item_acd_string)) return false;
		return true;
	}
	static bool checkPotion(char *name){
		//printf("name:%s\n",name);
		for(int i = potionLevel; i<=10;++i){
			//printf("potion name:%s\n",potionType[i]);
			if(strstr(name,potionType[i])){
				//printf("true\n");
				return true;
			}
		}
		//printf("false\n");
		return false;
	}
	static ACDActor * GetNearestACDItemByQuality(int MinItemValue, int Quality, int ItemLevel = 1, int Pages = 0, int Tomes=0, float Range = 9999.9f)
	{
		typedef int ( __cdecl * GetItemLevel)(int ACD_Ball_ID);	
		typedef int ( __cdecl *GetItemValue)(ACDActors::ACDActor *MyACD, int a, ACDActors::ACDActor *ACDPtr, int b);
		__try
		{
			float SaveDis = Range;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && isItem(GetACDActor(i)))
				{		
					if(GetAttributByString(GetACDActor(i),"GemQuality") == 0 && GetACDActor(i)->ItemLocation == -1 && GetAttributByString(GetACDActor(i),"Item_Quality_Level") >= Quality && ((GetItemLevel)pGetItemLevel)(GetACDActor(i)->id_acd_gBall) >= ItemLevel && (((GetItemValue)pItemValue)(GetACDActor(i),0,GetMyACD(),0) >= MinItemValue || GetAttributByString(GetACDActor(i),"Item_Quality_Level") > 4) || (GetACDActor(i)->ItemLocation == -1 && (Pages == 1 || Tomes ==1 ) && (is_page(GetACDActor(i)->id_sno) || is_tome(GetACDActor(i)->id_sno))))
					{
						if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis)
						{
							SaveDis = CurrDis;
							SaveActorNum = i;
						}
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}
	static ACDActor * GetNearestACDGEMByQuality(int Topaz, int Amethyst,int Smaragd,int Rubin, int Quality, float Range = 9999.9f)
	{
		if(Quality == 0) return NULL;
		typedef int ( __cdecl * GetItemLevel)(int ACD_Ball_ID);	
		__try
		{
			float SaveDis = Range;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i<= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && isItem(GetACDActor(i)))
				{				
					//1.000000 56916  Topaz_01-148
					//1.000000 56846  Ruby_01-149
					//1.000000 56888  Emerald_01-150
					//1.000000 56860  Amethyst_01-151
					if((ACDActors::inStr(GetACDActor(i)->Name,"Topaz") && Topaz) ||
						(ACDActors::inStr(GetACDActor(i)->Name,"Ruby") && Rubin) ||
						(ACDActors::inStr(GetACDActor(i)->Name,"Emerald") && Smaragd) ||
						(ACDActors::inStr(GetACDActor(i)->Name,"Amethyst") && Amethyst))
					{
						if(GetACDActor(i)->ItemLocation == -1 && GetAttributByString(GetACDActor(i),"GemQuality") >= Quality)
						{

							if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis)
							{
								SaveDis = CurrDis;
								SaveActorNum = i;
							}
						}
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}
	static int getMonsterCountWithin(int range){
		int count = 0;
		if(GetMyACD() == NULL) return 0;
			for(int i=0;i <= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && isMonster(GetACDActor(i)) && !ignoreActor(GetACDActor(i))){
					if( GetAttributByString(GetACDActor(i),"Hitpoints_Cur") &&
							!GetAttributByString(GetACDActor(i),"Is_NPC") &&
							!GetAttributByString(GetACDActor(i),"Untargetable") &&
							!GetAttributByString(GetACDActor(i),"Invulnerable") &&
							GetAttributByString(GetACDActor(i),"TeamID") != GetAttributByString(GetMyACD(),"TeamID"))
					{
						if(sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2)) <= range) ++count;
					}
				}
			}
			//printf("monsters:%d\n",count);
			return count;
	}
	static bool ignoreActor(ACDActors::ACDActor *npc){
		switch(npc->id_sno)
					{
						case 0x1395/*Raven_Pecking*/:
						case 0x1396/*Raven_Pecking*/:
						case 0xE4C8/*TEMP_A1C5_innMonster*/:
						case 0x1F941/*?????*/:
						case 0x2C3AF/*caldeumMiddleClass_Male_A_NoWanderNoTurn_Town*/:
						case 0x2AB34/*a3_Battlefield_demonic_forge*/:							
						case 0x1FFEB://BastionsKeepGuard_Melee_A_01?
						case 0x26D6D://bastionsKeepGuard_Melee_A_01_rmpt_atk_warning
						case 0x31001://bastionsKeepGuard_Melee_A_01_Frosty
						case 0x31002://bastionsKeepGuard_Melee_A_01_NPC_Frosty
						case 0x1CDF4://bastionsKeepGuard_Melee_A_01
						case 0x323BC://CaOut_Raven_Pecking_A
						case 0x323B2://CaOut_Raven_Pecking_B?
						case 0x2FF3D://CaldeumChild_Female_C  
						case 0x2FF53://CaldeumChild_Female_B_Follow
						case 0x2C43A://CaldeumChild_Male_A_Town
						case 0x2C44E://Caldeum Tortured_Poor_Male_C_T
						case 0x27FD8://Evacuation_Refugee_Cart
						case 0x2C3BD://caldeumTortured_Poor_Female_B_Town
						case 0x2C3C1://caldeumTortured_Poor_Male_G_Town
						case 0x2C3B5://caldeumPoor_Female_D_Town
						case 0x152://caldeumPoor_Male_E
						case 0x2C3BC://caldeumPoor_Male_F_Town     
						case 0x1585://SuperCaldeumGuard_Cleaver_A 
						case 0x31116://caldeumPoor_Male_A_Ambient 
						case 0x2F0AD://Nesrina 
						case 0x2F16E://Cyrus 
						case 0x2F16F://Javed
						case 0xC85://Asheara 
						case 0x2C661://caldeumGuard_Cleaver_A_Town 
						case 0x23415://caldeumEliteChapLady 
						case 0x302A0://caldeumPoor_Female_Shopper 
						case 0x31113://caldeumMiddleClass_Male_A_Ambient 
						case 0xE2C://caOut_Cage 
						case 0xE7FB://A2C2AlcarnusPrisoner 
						case 0x2F45F://A2C2AlcarnusPrisoner2 
						case 0x19B2://WoodWraith_sporeCloud_emitter  
						case 0x156A://Spore
						case 0x31214://caldeumChild_Male_A_Town_Bunny 
						case 0x1FFE8://bastionsKeepGuard_Melee_A_01_Corpse_01 
						case 0x1FFFE://bastionsKeepGuard_Ranged_A_01_Corpse_02 
						case 0x35B5D://bastionsKeepGuard_Melee_A_01_NoWander_Wounded 
						case 0x2C441://caldeumMiddleClass_Male_C_NoWanderNoTurn_Town 
						case 0x326E9://a3dun_Crater_DemonClawBomb_A_Monster
						case 0x14B06://trOut_Wilderness_Grave_Chest 
						case 0xD0D: //Beast_Corpse_A_02 
						case 0x32F54: //bastionsKeepGuard_Melee_A_01_NoWander_Dying 
						case 0x25E77://a3dun_crater_st_Demon_BloodContainer_A 
						return true;
						default:
						return false;
					}
	}
	static ACDActor * GetNearestAttackableActor(int MaxDis = 35)
	{
		__try
		{
			float SaveDis = 9999;
			float CurrDis = 0;
			int SaveActorNum = -1;
			if(GetMyACD() == NULL) return NULL;
			for(int i=0;i <= GetMaxACDActor();++i)
			{
				if(GetACDActor(i) != NULL && GetACDActor(i)->id_GUID != -1 && ACDActors::isMonster(GetACDActor(i)))
				{
					if(ignoreActor(GetACDActor(i))) continue;
					
					if((CurrDis = sqrt(pow(GetMyACD()->X - GetACDActor(i)->X,2)+pow(GetMyACD()->Y-GetACDActor(i)->Y,2))) < SaveDis && CurrDis < MaxDis)
					{
						if( GetAttributByString(ACDActors::GetACDActor(i),"Hitpoints_Cur") &&
							!GetAttributByString(ACDActors::GetACDActor(i),"Is_NPC") &&
							!GetAttributByString(ACDActors::GetACDActor(i),"Untargetable") &&
							!GetAttributByString(ACDActors::GetACDActor(i),"Invulnerable") &&
							GetAttributByString(ACDActors::GetACDActor(i),"TeamID") != GetAttributByString(GetMyACD(),"TeamID"))
						{
							SaveDis = CurrDis;
							SaveActorNum = i;
						}
					}
				}
			}
			if(SaveActorNum != -1)
				return GetACDActor(SaveActorNum);
			return NULL;
		} __except(1){return NULL;}
	}
	
	static void MoveItem(ACDActors::ACDActor* ACD, ACDActors::ItemLocation LocItem, int X, int Y)
	{
		struct sMoveItem
		{
			int UNK;
			ACDActors::ItemLocation LocItem;
			int X;
			int Y;
		};
		sMoveItem *MoveStruct = new sMoveItem();
		typedef int ( __cdecl * MoveItem)(ACDActors::ACDActor* ACD, sMoveItem* MoveS, int alwaysZero);
		MoveStruct->UNK = ACD->id_owner;
		MoveStruct->LocItem = LocItem;
		MoveStruct->X = X;
		MoveStruct->Y = Y;

		((MoveItem)pMoveItem)(ACD,MoveStruct,0);
	 }
	static int StashItems(int AllowedStacks, int sellPotions)
	{
		typedef int ( __cdecl *stashItem)(ACDActors::ACDActor *ItemACD, unsigned long *PlayerACDId, unsigned long *Tab);
		typedef struct  {
			unsigned long PlayerACDID;
			unsigned long _location;
			unsigned long _position_x;
			unsigned long _position_y;
		}ItemPosition_PACKET;
		ItemPosition_PACKET Item;
		Item.PlayerACDID = GetMyACD()->id_acd;
		Item._location = ACDActors::Stash;
		int PotionStacks = 0;
		unsigned long *tabs[] = {(unsigned long*)0,(unsigned long*)10,(unsigned long*)20};
		try
		{
			for(int g=0;g<3;++g){
				PotionStacks = 0;
				for(int i=0;i<= ACDActors::GetMaxACDActor();++i)
				{
					if((ACDActors::GetACDActor(i) != NULL && isItem(ACDActors::GetACDActor(i)) && ACDActors::GetACDActor(i)->ItemLocation == ACDActors::Inventory))
					{
						if(checkPotion(ACDActors::GetACDActor(i)->Name)) ++PotionStacks;
						if(!checkPotion(ACDActors::GetACDActor(i)->Name) || (PotionStacks > AllowedStacks && sellPotions == 0)){
							((stashItem)pStashItem)(ACDActors::GetACDActor(i),&Item.PlayerACDID,tabs[g]);
						}
					}
				}
			}
			return 1;
		}
		catch (...)
		{
			return 0;
		}
	}

	static int SellItems(int AllowedStacks, int sellPotions,int item_quality)
	{
		int PotionStacks = 0;
		try
		{
			for(int i=0;i<= ACDActors::GetMaxACDActor();++i)
			{
				if((ACDActors::GetACDActor(i) != NULL && isItem(ACDActors::GetACDActor(i)) && ACDActors::GetACDActor(i)->ItemLocation == ACDActors::Inventory))
				{
					 /*some custom shit lol
					int crit_damage = (int)(ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Crit_Damage_Percent")*100);

					if((int)ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Damage_Weapon_Average_Total_All") > 800 && (int)ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Sockets") > 0 && crit_damage > 85 && crit_damage <= 100){
						printf("average damage:%d\n",(int)ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Damage_Weapon_Average_Total_All"));
						printf("sockets:%d\n",(int)ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Sockets"));
						continue;
					}//*/
					if(ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"GemQuality") != 0) {
						//printf("GEM SKIP!\n");
						continue;
					}
					if(ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Item_Quality_Level") > item_quality){
						//printf("QUALITY SKIP!:%f>%d\n",ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Item_Quality_Level"),item_quality);
						continue;
					}
					if(!checkLootTable(ACDActors::GetACDActor(i))){
						//printf("LOOT TABLE SKIP!\n");
						continue;
					}
					if(checkPotion(ACDActors::GetACDActor(i)->Name)) ++PotionStacks;
					if(!checkPotion(ACDActors::GetACDActor(i)->Name) || (PotionStacks > AllowedStacks && sellPotions == 1)){
						SellItem(ACDActors::GetACDActor(i));
					}
				}
			}
			memset(itemIdsInLootTable,0x00,strlen(itemIdsInLootTable));
			return 1;
		}
		catch (...)
		{
			return 0;
		}
	}
};

class SelectQuestClass
{
public:
	static int Flag;
	static int QuestID;
	static int SubQuestID;
	static int Difficulty;
	static int Act;
	static int StartResume; // 0 = Start 1= Resume
	static void SetMonsterLevel(int Level)
	{
		__try
		{
			*(DWORD*)(*(DWORD*)(*(DWORD*)(*(DWORD*)pMonsterLevel+0x10)+0x10)+0x1C) = Level;
		}__except(1){}
	}
	static void SetQuest(int Difficulty, int Act, int QuestID, int SubQuestID, int StartResume)
	{
		typedef int ( __cdecl * OpenQuestDialog)();
		typedef int ( __cdecl * QuestAccept)();
		SelectQuestClass::Difficulty = Difficulty;
		((OpenQuestDialog)pOpenQuestDialog)();
		SelectQuestClass::Act = Act;
		SelectQuestClass::QuestID = QuestID; 
		SelectQuestClass::SubQuestID = SubQuestID;
		SelectQuestClass::StartResume = StartResume;
		SelectQuestClass::Flag = 1;			
		*(DWORD*)((*(DWORD*)(pQuestSelectFix))+0x18) = 1;
		QuestHook->Hook();
		((QuestAccept)pQuestAccept)();
		QuestHook->UnHook();
	}
};
int SelectQuestClass::Flag = 0;
int SelectQuestClass::QuestID = 0;
int SelectQuestClass::SubQuestID = 0;
int SelectQuestClass::Difficulty = 0;
int SelectQuestClass::Act = 0;
int SelectQuestClass::StartResume = 0;



void UseWaypoint(int SelectWaypointIndex)
{
	// Remove Exeptionhandler
	DWORD dwOldProtection;
	VirtualProtect(pSelectWaypoint+0xAA, 5, PAGE_EXECUTE_READWRITE, &dwOldProtection);
	memset(pSelectWaypoint+0xAA, 0x90, 5);
	VirtualProtect(pSelectWaypoint+0xAA, 5, dwOldProtection, &dwOldProtection);

	_asm MOV EDI,SelectWaypointIndex
	_asm MOV EAX,SelectWaypointIndex
	_asm call pSelectWaypoint	
}

_declspec(naked) void QuestChangeHook()
{
	_asm PUSH EBP 
	_asm MOV EBP,ESP
	_asm PUSH -1

	_asm pushad
	DWORD EAXSave;
	_asm mov EAXSave, eax
	static DWORD retOriginalFunc = (DWORD)(QuestHook->HookLocation + QuestHook->Size);
	if(SelectQuestClass::Flag == 1)
	{
		*(DWORD*)(EAXSave+0x14) = SelectQuestClass::Difficulty; 
		*(DWORD*)(EAXSave+0x18) = SelectQuestClass::Act;
		*(DWORD*)(EAXSave+0x1C) = SelectQuestClass::QuestID;
		*(DWORD*)(EAXSave+0x20) = SelectQuestClass::SubQuestID;
		*(DWORD*)(EAXSave+0x24) = SelectQuestClass::StartResume;
		SelectQuestClass::Flag = 0;
	}
	_asm popad

	_asm jmp retOriginalFunc
}

_declspec(naked) void UsePower(int SNO, ACDActors::ACDActor* ACDPtr, float X = -1, float Y = -1, ULONG ACDID = -1)
{
		typedef struct {
			DWORD power_1;
			DWORD power_2;
			DWORD cmd;
			ULONG acd_id;
			float x, y, z;
			DWORD world_id;
			DWORD end;
			DWORD zero;
	} POWER_PACKET;
	__asm push ebp
	__asm mov ebp, esp
	__asm sub esp, 0x80
	__asm pushad
	__asm pushad
	DWORD PlayerActorAddress, *pPlayerActorAddress;
	pPlayerActorAddress = &PlayerActorAddress;

	POWER_PACKET iPacket, *piPacket;
	piPacket = &iPacket;
	PlayerActorAddress = ACDActors::GetRActorPtrFromGUID(MyACD::GetMyACD()->id_GUID);
	if(SNO == 30021 || SNO == 30022) // Interact
	{
		piPacket->power_1 = piPacket->power_2 = SNO;
		piPacket->acd_id = ACDPtr->id_acd;
		piPacket->x = piPacket->y = piPacket->z = piPacket->world_id = 0;
		piPacket->cmd = 1;
	}
	else if(X == -1 && Y == -1) //Skillcast
	{
		piPacket->power_1 = piPacket->power_2 = SNO;
		piPacket->acd_id = ACDID;
		piPacket->x = ACDPtr->X;
		piPacket->y = ACDPtr->Y;
		piPacket->z = ACDPtr->Z;
		piPacket->cmd = ((ACDID == -1)?2:1);
		piPacket->world_id = MyACD::GetMyACD()->id_world;
	}
	else // MoveTo
	{
		piPacket->power_1 = piPacket->power_2 = 0x0000777C;
		piPacket->cmd = 2;
		piPacket->acd_id = 0xFFFFFFFF;
		piPacket->x = X;
		piPacket->y = Y;
		piPacket->z = MyACD::GetMyACD()->Z;
		piPacket->world_id = MyACD::GetMyACD()->id_world;
	}
	piPacket->end = 0xFFFFFFFF;
	piPacket->zero = 0;	
	__asm popad

	__asm push pPlayerActorAddress
	__asm push 1
	__asm push 1
	__asm mov esi, piPacket
	__asm mov eax, PlayerActorAddress
	__asm mov ecx, pUsePowerToLocation
	__asm call ecx
	__asm mov esp, ebp 
	__asm pop ebp

	__asm ret
}

_declspec(naked) void UsePowerToPosition(int SNO, float X, float Y, float Z)
{
		typedef struct {
			DWORD power_1;
			DWORD power_2;
			DWORD cmd;
			ULONG acd_id;
			float x, y, z;
			DWORD world_id;
			DWORD end;
			DWORD zero;
	} POWER_PACKET;
	__asm push ebp
	__asm mov ebp, esp
	__asm sub esp, 0x80
	__asm pushad
	__asm pushad
	DWORD PlayerActorAddress, *pPlayerActorAddress;
	pPlayerActorAddress = &PlayerActorAddress;

	POWER_PACKET iPacket, *piPacket;
	piPacket = &iPacket;
	PlayerActorAddress = ACDActors::GetRActorPtrFromGUID(MyACD::GetMyACD()->id_GUID);

	piPacket->power_1 = piPacket->power_2 = SNO;
	piPacket->acd_id = 0xFFFFFF;
	piPacket->x = X;
	piPacket->y = Y;
	piPacket->z = Z;
	piPacket->cmd = 2;
	piPacket->world_id = MyACD::GetMyACD()->id_world;

	piPacket->end = 0xFFFFFFFF;
	piPacket->zero = 0;	
	__asm popad

	__asm push pPlayerActorAddress
	__asm push 1
	__asm push 1
	__asm mov esi, piPacket
	__asm mov eax, PlayerActorAddress
	__asm mov ecx, pUsePowerToLocation
	__asm call ecx
	__asm mov esp, ebp 
	__asm pop ebp

	__asm ret
}
#include "NamedPipe.h"
#include "HideModule.h"