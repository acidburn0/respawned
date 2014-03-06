#include<Windows.h>
#include <Time.h>
#include <queue>

std::queue<HANDLE> D3MailQueueHandle; 

struct GWHeader{
	unsigned short header;
	struct MyType{
		int 	i;
		float 	f;
	} p_1, p_2, p_3, p_4, p_5, p_6, p_7, p_8, p_9, p_10,
	  r_1, r_2, r_3, r_4, r_5, r_6, r_7, r_8, r_9, r_10, r_11;
	char StringBuffer[100];
	char StringEmail[100];
	char StringPassword[100];
	struct sD3Info
	{
		DWORD GraphicFlag;
		char PassedLootTablePath[261];
		char PassedSpellRulePath[261];
		struct sSettings
		{
            int State;
            int AttackRange;
            int MoneyMinAmount;
			int MinItemValue;
            int LootItemQuality;
            int SellItemQuality;
            int ItemMinLevel;
            int BlessedShrine;
            int FrenziedShrine;
            int FortuneShrine;
            int EnlightenedShrine;
			int EmpoweredShrine;
            int FleetingShrine;
			int HealingWell;
            int OpenChests;
            int Repair;
            int SellSalvage;
			int Topaz;
			int Amethyst;
			int Emerald;
			int Ruby;
			int Pages;
			int Tomes;
			int LootTable;
			int GEM_Quality;
			int UsePotion;
			int UsePotionByPercent;
			int UseHealingWellsAt;
			int PotionLevel;
			int UnstuckSpell;
			int SpellRulesEnabled;
		} Settings;
		int PTR;
		ACDActors::ACDActor* ACDPTR;
		char Name[100];
		float X;
		float Y;
		float Z;
		ULONG GUID;
		ULONG ACDGUID;
		ULONG ModelID;
		float HP;
		float HP_max;
		int playerClass;
		int resource;
		int resource2;
		int Ghosted;
		bool Dead;
		int Level;
		int ParagonLevel;
		int BackpackFreeSlots;
		int BackbackFreeDoubleSlot;
		int GGRadius;
		int GF;
		int Gold;
		int XP;
		int isMoving;
		bool ReadyToStartQuest;
		int Durability;
		int QuestStep;
		bool IsDisconnected;
		int InGame;
		int inTown;
		int Act;
		int QuestID;
		int SubQuestID;
		int MonsterLevel;
		int SellPotions;
		int PotionStacksAllowed;
		struct Unit
		{
			DWORD Type;
			char Name[100];
			float X,Y,Z;
			ULONG GUID;
			ULONG ACDGUID;
			ULONG ModelID;
			ACDActors::ACDActor* ACDPTR;
		}Actor[500], ActorByName, ActorByModelID, retUnit;
	}D3Info;
	GWHeader(){memset(this,0,sizeof(GWHeader));}
}D3Mail;
		
enum COMMANDS{
	D3_Update, D3_Activate, D3_Login, D3_SelectQuestStart,D3_SelectQuestResume, 
	D3_GetACDActor, D3_UsePowerToActor, D3_AttackMonster, 
	D3_UseTownPortal, D3_UseWaypoint, D3_Repair, D3_LeaveWorld, D3_Revival, 
	D3_MoveTo, D3_PickUpMoney, 
	D3_GetNearestItemByQuality, D3_GetNearestGEMByQuality, D3_GetNearestPotionByQuality, D3_SellItemsLowerQuality, D3_IdentifyBackPack, 
	D3_GetNearestChest, D3_GetNearestShrine, 
	D3_UsePotion, D3_CastSpellToUnstuck, D3_SkipScene,D3_GetItemOnLootList, D3_Stash, D3_TakeScreenShot
};	


time_t lastPotion = NULL;
char HackBit[5] = {'\0','\0','\0','\0','\0'};
int skillcount=-1;
int iteration = 0;

int exFilter(unsigned int code, struct _EXCEPTION_POINTERS *ep, int header) {
	printf("ErrorCode:%08X\nIn Header:%d\nAt:%08X\n",code,header,ep->ExceptionRecord->ExceptionAddress);
	return 1;
}

void SendRandomlyTimedEnter(){
	Sleep(((rand() %(50-10)) + 10));
	PostMessageA(((GetHWND)pGetHWND)(),WM_KEYDOWN,VK_RETURN,0);
	Sleep(((rand() %(50-20)) + 20));
	PostMessageA(((GetHWND)pGetHWND)(),WM_KEYUP,VK_RETURN,1);
}

int CheckIfSomethingToSell(){
	for(int i=0;i<= ACDActors::GetMaxACDActor();++i)
	{
		if(ACDActors::GetACDActor(i) != NULL && ACDActors::GetACDActor(i)->ItemLocation == ACDActors::Inventory && ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"GemQuality") == 0 &&
			ACDActors::GetAttributByString(ACDActors::GetACDActor(i),"Item_Quality_Level") < D3Mail.D3Info.Settings.SellItemQuality)
		{
			return 1;
		}
	}
	return 0;
}
void checkAuthThread(){
	DWORD atid;
	DWORD dtid;
    GetExitCodeThread(hThread_auth_check,&atid);
	GetExitCodeThread(hThread_Anti_debug,&dtid);
    if(atid == 0 || dtid == 0 || hThread_Anti_debug == NULL || hThread_auth_check == NULL) {
		raise(SIGTERM);
	}
}

bool contains_number(char* dt)
{
	std::string c = dt;
	return (c.find_first_of("0123456789") != std::string::npos);
}  

void InstanceThread(){
	if(D3MailQueueHandle.empty()){
		//printf("empty queue.\n");
		return;
	}
	//printf("header:%d\n",D3Mail.header);
	//printf("GameState:%d\n",D3Mail.D3Info.Settings.State);
	HANDLE hPipe = D3MailQueueHandle.front();
	D3MailQueueHandle.pop();
    DWORD cbBytesRead = 0, cbReplyBytes = 0, cbWritten = 0;
    BOOL fSuccess = ReadFile(hPipe,&D3Mail, sizeof(D3Mail),&cbBytesRead,NULL);
	time_t Time = NULL;
	int currentHealthPercent;
	int PotionStackCount = 0;
	char deathText[256];
	if (fSuccess && cbBytesRead != 0)
	{
		Sleep(10);
		//printf("respawn path:%s\n",RespawnPath);
		D3Mail.r_1.f = D3Mail.r_2.f = D3Mail.r_3.f = D3Mail.r_4.f = D3Mail.r_5.f = D3Mail.r_6.f = D3Mail.r_7.f = D3Mail.r_8.f = D3Mail.r_9.f = D3Mail.r_10.f = 0.0f;
		D3Mail.r_1.i = D3Mail.r_2.i = D3Mail.r_3.i = D3Mail.r_4.i = D3Mail.r_5.i = D3Mail.r_6.i = D3Mail.r_7.i = D3Mail.r_8.i = D3Mail.r_9.i = D3Mail.r_10.i = 0;
					checkAuthThread();
					HeapCreateHook->Hook(); // Disable Warden
					CreateFileHook->UnHook();
			__try
			{
				int Counter = 0;
				for(int i=0;i<(short)ACDActors::GetMaxACDActor();++i)
				{
					if((short)ACDActors::GetMaxACDActor() == -1 || Counter >= 500) break;
					if((D3Mail.D3Info.Actor[Counter].ACDPTR = ACDActors::GetACDActor(i)) != NULL)
					{
						D3Mail.D3Info.Actor[Counter].Type  = ACDActors::GetUnitTypeFromSnoID(D3Mail.D3Info.Actor[i].ACDPTR, "Actor");
						D3Mail.D3Info.Actor[Counter].X = D3Mail.D3Info.Actor[i].ACDPTR->X;
						D3Mail.D3Info.Actor[Counter].Y = D3Mail.D3Info.Actor[i].ACDPTR->Y;
						D3Mail.D3Info.Actor[Counter].Z = D3Mail.D3Info.Actor[i].ACDPTR->Z;
						D3Mail.D3Info.Actor[Counter].GUID = D3Mail.D3Info.Actor[i].ACDPTR->id_GUID;
						D3Mail.D3Info.Actor[Counter].ACDGUID = D3Mail.D3Info.Actor[i].ACDPTR->id_acd;
						D3Mail.D3Info.Actor[Counter].ModelID = D3Mail.D3Info.Actor[i].ACDPTR->id_sno;
						strcpy(D3Mail.D3Info.Actor[Counter].Name, D3Mail.D3Info.Actor[i].ACDPTR->Name);
						++Counter;
					}
				}
				D3Mail.D3Info.InGame = 0;
				D3Mail.D3Info.Settings.State = 0;
				if((D3Mail.D3Info.ACDPTR = MyACD::GetMyACD()) != NULL)
				{
					//printf("potion used Last:%d\n",ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Last_Tick_Potion_Used"));
					D3Mail.D3Info.InGame = 1;
					skillcount = MyACD::GetSkillCount();
					strcpy(D3Mail.D3Info.Name, D3Mail.D3Info.ACDPTR->Name);
					D3Mail.D3Info.ModelID = D3Mail.D3Info.ACDPTR->id_sno;
					D3Mail.D3Info.GUID = D3Mail.D3Info.ACDPTR->id_GUID;
					D3Mail.D3Info.ACDGUID = D3Mail.D3Info.ACDPTR->id_acd;
					D3Mail.D3Info.X = D3Mail.D3Info.ACDPTR->X;
					D3Mail.D3Info.Y = D3Mail.D3Info.ACDPTR->Y;
					D3Mail.D3Info.Z = D3Mail.D3Info.ACDPTR->Z;
					D3Mail.D3Info.BackpackFreeSlots = MyACD::BackpackFreeSlots();
					D3Mail.D3Info.BackbackFreeDoubleSlot = MyACD::BackbackFreeDoubleSlot();
					D3Mail.D3Info.Gold = MyACD::GetGold();
					D3Mail.D3Info.XP = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"");
					D3Mail.D3Info.Durability = (int)MyACD::DurabilityPercent();
					D3Mail.D3Info.GF = (int)MyACD::GetGF();
					D3Mail.D3Info.HP = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Hitpoints_Cur");
					D3Mail.D3Info.HP_max = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Hitpoints_Max_Total");
					D3Mail.D3Info.playerClass = MyACD::GetPlayerClass();
					D3Mail.D3Info.resource = MyACD::getResources(D3Mail.D3Info.playerClass);
					if(D3Mail.D3Info.playerClass == 5)D3Mail.D3Info.resource2 = MyACD::getResources(6);
					currentHealthPercent = (int)(((float)((DWORD)D3Mail.D3Info.HP) / (float)((DWORD)D3Mail.D3Info.HP_max))*100);
					D3Mail.D3Info.Ghosted = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Ghosted");
					//printf("Ghosted:%d\n",D3Mail.D3Info.Ghosted);
					D3Mail.D3Info.Level = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Level");
					D3Mail.D3Info.ParagonLevel = ACDActors::GetAttributByString(D3Mail.D3Info.ACDPTR,"Alt_Level");
					MyACD::GetQuest(&D3Mail.D3Info.Act,&D3Mail.D3Info.QuestID, &D3Mail.D3Info.QuestStep,&D3Mail.D3Info.SubQuestID);
					D3Mail.D3Info.inTown = !MyACD::IsSkillReady(0x0002EC66);
					D3Mail.D3Info.GGRadius = MyACD::GetGFRadius();
					potionLevel = D3Mail.D3Info.Settings.PotionLevel;
					//printf("path:%s\n",D3Mail.D3Info.Settings.RespawnPath);
					//printf("buffer:%s\n",lootTableBuffer);
					//printf("under review:%f\n",ACDActors::GetAttributByString(D3Mail.D3Info.Actor->ACDPTR,"Account_Under_Review"));
					//printf("Dead:%d\n",D3Mail.D3Info.Dead);
					D3Mail.D3Info.Dead = false;
					if(!MyACD::SkillAktive(0x0002EC66)){
						if(D3Mail.D3Info.Settings.UsePotion == 1 && D3Mail.D3Info.Settings.UsePotionByPercent > currentHealthPercent && MyACD::UsePotion(true) == 1 && (lastPotion == NULL || (time(NULL)-lastPotion) >= 35)){
							D3Mail.D3Info.Settings.State = 11; // Time to usePotion
							lastPotion = time(NULL);
						}
						else if(MyACD::GetNearestShrine(D3Mail.D3Info.Settings.BlessedShrine,
														D3Mail.D3Info.Settings.EnlightenedShrine,
														D3Mail.D3Info.Settings.FortuneShrine,
														D3Mail.D3Info.Settings.FrenziedShrine,
														D3Mail.D3Info.Settings.EmpoweredShrine,
														D3Mail.D3Info.Settings.FleetingShrine,
														D3Mail.D3Info.Settings.HealingWell,
														D3Mail.D3Info.Settings.UseHealingWellsAt,
														currentHealthPercent) != NULL)
							D3Mail.D3Info.Settings.State = 3;
						else if(MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) != NULL)
							D3Mail.D3Info.Settings.State = 1; // Attack
						else if(D3Mail.D3Info.BackbackFreeDoubleSlot && D3Mail.D3Info.Settings.LootTable && MyACD::GetACDInLootTable() != NULL && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 13; // loot from loot table
						else if(D3Mail.D3Info.Settings.Repair > D3Mail.D3Info.Durability)
							D3Mail.D3Info.Settings.State = 9; //Repair
						else if(D3Mail.D3Info.Settings.SellItemQuality != 11 && D3Mail.D3Info.BackbackFreeDoubleSlot == 0 && MyACD::GetNearestACDItemByQuality(D3Mail.D3Info.Settings.MinItemValue,D3Mail.D3Info.Settings.LootItemQuality,D3Mail.D3Info.Settings.ItemMinLevel) != NULL && CheckIfSomethingToSell() > 0)
							D3Mail.D3Info.Settings.State = 7; // Sell
						else if(D3Mail.D3Info.Settings.OpenChests && MyACD::GetNearestChest() != NULL && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 4; //Chest
						else if(D3Mail.D3Info.BackbackFreeDoubleSlot && MyACD::GetNearestACDGEMByQuality(D3Mail.D3Info.Settings.Topaz,D3Mail.D3Info.Settings.Amethyst,D3Mail.D3Info.Settings.Emerald,D3Mail.D3Info.Settings.Ruby,D3Mail.D3Info.Settings.GEM_Quality) != NULL && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 10; // Pickuptem GEM
						else if(D3Mail.D3Info.BackbackFreeDoubleSlot && MyACD::GetNearestACDPotion(D3Mail.D3Info.Settings.PotionLevel) != NULL && D3Mail.D3Info.Settings.PotionLevel != -1 && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 12; // Pickuptem Potion
						else if(D3Mail.D3Info.BackbackFreeDoubleSlot && MyACD::GetNearestACDItemByQuality(D3Mail.D3Info.Settings.MinItemValue,D3Mail.D3Info.Settings.LootItemQuality,D3Mail.D3Info.Settings.ItemMinLevel,D3Mail.D3Info.Settings.Pages,D3Mail.D3Info.Settings.Tomes) != NULL && D3Mail.D3Info.Settings.LootItemQuality != -1 && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 5; // Pickuptem Item
						else if(MyACD::GetNearestACDMoney(D3Mail.D3Info.Settings.MoneyMinAmount) != NULL && MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange) == NULL)
							D3Mail.D3Info.Settings.State = 2; // Money
					}
				}
				if(GetAsyncKeyState(VK_F11) < 0) {
					D3Mail.D3Info.Settings.State = 7;
					char skillfile[64] = {0};
					sprintf(skillfile,"C:\\%02X%02X%02X%02Xskills-%02X.txt\x00",(rand()%255),(rand()%255),(rand()%255),(rand()%255),D3Mail.D3Info.playerClass);
					char skillid[30] = {0x00};
					FILE *sffp = fopen(skillfile,"w+");
					for(int i = 0; i <6;++i){
						fprintf(sffp,"slot%d: %d\n",i,MyACD::GetSkillID(i));
					}
					fclose(sffp);
				}
				if(UIElements::GetVisibility("Root.NormalLayer.deathmenu_dialog.dialog_main.button_revive_at_checkpoint")){
					strcpy(deathText,UIElements::GetUI("Root.NormalLayer.deathmenu_dialog.dialog_main.button_revive_at_checkpoint")->pntrText);
					if(!contains_number(deathText)){
						D3Mail.D3Info.Dead = true;
						D3Mail.D3Info.Settings.State = 8; // Tod
					}
				}
				if(UIElements::GetVisibility("Root.TopLayer.BattleNetModalNotifications_main.ModalNotification.Buttons.ButtonList.OkButton")){
					char *errorText = UIElements::GetUI("Root.TopLayer.BattleNetModalNotifications_main.ModalNotification.Content.List.Message")->pntrText;
					if(!strstr(errorText,"300005") && !strstr(errorText,"317000") && !strstr(errorText,"316609") /*&& !strstr(errorText,"316611")*/){
						D3Mail.D3Info.IsDisconnected = true;
						//system("pause");
					} else {
						SendRandomlyTimedEnter();
					}
				}
				D3Mail.D3Info.ReadyToStartQuest = ((UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Slot1.LayoutRoot.SwitchHero") || UIElements::GetVisibility("Root.NormalLayer.BattleNetCampaign_main.LayoutRoot.Menu.ChangeQuestButton")) && !D3Mail.D3Info.IsDisconnected);
				//printf("Disconnected:%d\n",D3Mail.D3Info.IsDisconnected);
				//printf("actor by name:%s\n",D3Mail.D3Info.ActorByName.Name);
				if((D3Mail.D3Info.ActorByName.Name != "" && (D3Mail.D3Info.ActorByName.ACDPTR = ACDActors::GetNearestACDActorByName(D3Mail.D3Info.ActorByName.Name)) != NULL))
				{
					D3Mail.D3Info.ActorByName.X = D3Mail.D3Info.ActorByName.ACDPTR->X;
					D3Mail.D3Info.ActorByName.Y = D3Mail.D3Info.ActorByName.ACDPTR->Y;
					D3Mail.D3Info.ActorByName.Z = D3Mail.D3Info.ActorByName.ACDPTR->Z;
					D3Mail.D3Info.ActorByName.GUID = D3Mail.D3Info.ActorByName.ACDPTR->id_GUID;
					D3Mail.D3Info.ActorByName.ACDGUID = D3Mail.D3Info.ActorByName.ACDPTR->id_acd;
					D3Mail.D3Info.ActorByName.ModelID = D3Mail.D3Info.ActorByName.ACDPTR->id_sno;
				}
				else
				{
					strcpy(D3Mail.D3Info.ActorByName.Name, "");
					D3Mail.D3Info.ActorByName.X = 0.0f;
					D3Mail.D3Info.ActorByName.Y = 0.0f;
					D3Mail.D3Info.ActorByName.Z = 0.0f;
					D3Mail.D3Info.ActorByName.GUID = 0;
					D3Mail.D3Info.ActorByName.ACDGUID = 0;
				}
				if((D3Mail.D3Info.ActorByModelID.ACDPTR = ACDActors::GetNearestActorByModelID(D3Mail.D3Info.ActorByModelID.ModelID)))
				{
					strcpy(D3Mail.D3Info.ActorByModelID.Name, D3Mail.D3Info.ActorByModelID.ACDPTR->Name);
					D3Mail.D3Info.ActorByModelID.X = D3Mail.D3Info.ActorByModelID.ACDPTR->X;
					D3Mail.D3Info.ActorByModelID.Y = D3Mail.D3Info.ActorByModelID.ACDPTR->Y;
					D3Mail.D3Info.ActorByModelID.Z = D3Mail.D3Info.ActorByModelID.ACDPTR->Z;
					D3Mail.D3Info.ActorByModelID.GUID = D3Mail.D3Info.ActorByModelID.ACDPTR->id_GUID;
					D3Mail.D3Info.ActorByModelID.ACDGUID = D3Mail.D3Info.ActorByModelID.ACDPTR->id_acd;
				}
				else
				{
					strcpy(D3Mail.D3Info.ActorByModelID.Name, "");
					D3Mail.D3Info.ActorByModelID.X = 0.0f;
					D3Mail.D3Info.ActorByModelID.Y = 0.0f;
					D3Mail.D3Info.ActorByModelID.Z = 0.0f;
					D3Mail.D3Info.ActorByModelID.GUID = 0;
					D3Mail.D3Info.ActorByModelID.ACDGUID = 0;
				}
				strcpy(D3Mail.D3Info.retUnit.Name, "");
				D3Mail.D3Info.retUnit.X = 0.0f;
				D3Mail.D3Info.retUnit.Y = 0.0f;
				D3Mail.D3Info.retUnit.Z = 0.0f;
				D3Mail.D3Info.retUnit.GUID = 0;
				D3Mail.D3Info.retUnit.ACDGUID = 0;
				switch (D3Mail.header)
				{
					case  D3_UsePotion:
						D3Mail.r_1.i = MyACD::UsePotion();
						break;
					//case D3_IdentifyBackPack:
					//	D3Mail.r_1.i = MyACD::IdentifyItems();
					//	break;
					case D3_SellItemsLowerQuality:
						D3Mail.r_1.i = MyACD::SellItems(D3Mail.D3Info.PotionStacksAllowed,D3Mail.D3Info.SellPotions,D3Mail.D3Info.Settings.SellItemQuality);
						break;
					case D3_GetNearestPotionByQuality:
						ACDActors::ACDActor * ACD_Potion;
						D3Mail.r_1.i = 0;
						if(MyACD::BackbackFreeDoubleSlot() == 1 && (ACD_Potion = MyACD::GetNearestACDPotion(D3Mail.D3Info.Settings.PotionLevel)) != NULL)
						{		
							strcpy(D3Mail.D3Info.retUnit.Name, ACD_Potion->Name);
							if(sqrt(pow(ACD_Potion->X-MyACD::GetMyACD()->X,2)+pow(ACD_Potion->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								UsePower(30021,ACD_Potion);	
							}
							else
							{
								UsePower(NULL, NULL, ACD_Potion->X,ACD_Potion->Y);
							}
							D3Mail.r_1.i = 1;
						}
						break;
					case D3_GetNearestGEMByQuality:
						ACDActors::ACDActor * GEM_ACD;
						D3Mail.r_1.i = 0;
						if(MyACD::BackbackFreeDoubleSlot() == 1 && (GEM_ACD = MyACD::GetNearestACDGEMByQuality(D3Mail.D3Info.Settings.Topaz,D3Mail.D3Info.Settings.Amethyst,D3Mail.D3Info.Settings.Emerald,D3Mail.D3Info.Settings.Ruby,D3Mail.D3Info.Settings.GEM_Quality)) != NULL)
						{		
							strcpy(D3Mail.D3Info.retUnit.Name, GEM_ACD->Name);
							if(sqrt(pow(GEM_ACD->X-MyACD::GetMyACD()->X,2)+pow(GEM_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								UsePower(30021,GEM_ACD);	
							}
							else
							{
								UsePower(NULL, NULL, GEM_ACD->X,GEM_ACD->Y);
							}
							D3Mail.r_1.i = 1;
						}
						break;
					case D3_GetNearestItemByQuality:
						ACDActors::ACDActor * Item_ACD;
						D3Mail.r_1.i = 0;
						if(MyACD::BackbackFreeDoubleSlot() == 1 && (Item_ACD = MyACD::GetNearestACDItemByQuality(D3Mail.D3Info.Settings.MinItemValue,D3Mail.D3Info.Settings.LootItemQuality, D3Mail.D3Info.Settings.ItemMinLevel,D3Mail.D3Info.Settings.Pages,D3Mail.D3Info.Settings.Tomes)) != NULL)
						{		
							strcpy(D3Mail.D3Info.retUnit.Name, Item_ACD->Name);
							if(sqrt(pow(Item_ACD->X-MyACD::GetMyACD()->X,2)+pow(Item_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								/*if((lastScreenShot == NULL || (time(NULL)-lastScreenShot) >= 20) && ACDActors::GetAttributByString(Item_ACD,"Item_Quality_Level") >= D3Mail.D3Info.Settings.SellItemQuality) {
									MyACD::TakeScreenShot();
									lastScreenShot = time(NULL);
								}*/
								UsePower(30021,Item_ACD);	
							}
							else
							{
								UsePower(NULL, NULL, Item_ACD->X,Item_ACD->Y);
							}
							D3Mail.r_1.i = 1;
						}
						/* to be "enabled/disabled" via option.. (takes a screen of legendary drops)*/
						break;
					case D3_TakeScreenShot:
						//D3Mail.r_1.i = MyACD::TakeScreenShot();
						break;
					case D3_GetItemOnLootList:
						ACDActors::ACDActor * Loot_ACD;
						char loot_acd_string[32];
						D3Mail.r_1.i = 0;
						Loot_ACD = MyACD::GetACDInLootTable();
						if((MyACD::BackbackFreeDoubleSlot() == 1 || Loot_ACD->id_sno == 85798) && Loot_ACD != NULL)
						{		
							strcpy(D3Mail.D3Info.retUnit.Name, Loot_ACD->Name);
							sprintf(loot_acd_string,"%08X\n",Loot_ACD->id_acd_gBall);
							if(!strstr(itemIdsInLootTable,loot_acd_string)) strcat(itemIdsInLootTable,loot_acd_string);
							if(sqrt(pow(Loot_ACD->X-MyACD::GetMyACD()->X,2)+pow(Loot_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								UsePower(30021,Loot_ACD);	
							}
							else
							{
								UsePower(NULL, NULL, Loot_ACD->X,Loot_ACD->Y);
							}
							D3Mail.r_1.i = 1;
						} 
						break;
					case D3_GetNearestChest:
						ACDActors::ACDActor * Chest_ACD;
						D3Mail.r_1.i = 0;
						if((Chest_ACD = MyACD::GetNearestChest()) != NULL)
						{		
							strcpy(D3Mail.D3Info.retUnit.Name, Chest_ACD->Name);
							if(sqrt(pow(Chest_ACD->X-MyACD::GetMyACD()->X,2)+pow(Chest_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								UsePower(30021,Chest_ACD);	
							}
							else
							{
								UsePower(NULL, NULL, Chest_ACD->X,Chest_ACD->Y);
							}
							D3Mail.r_1.i = 1;
						}
						break;
					case D3_GetNearestShrine:
						ACDActors::ACDActor * Shrine_ACD;
						D3Mail.r_1.i = 0;
						
						if((Shrine_ACD = MyACD::GetNearestShrine(D3Mail.D3Info.Settings.BlessedShrine,D3Mail.D3Info.Settings.EnlightenedShrine,D3Mail.D3Info.Settings.FortuneShrine,D3Mail.D3Info.Settings.FrenziedShrine,D3Mail.D3Info.Settings.EmpoweredShrine,D3Mail.D3Info.Settings.FleetingShrine,D3Mail.D3Info.Settings.HealingWell,D3Mail.D3Info.Settings.UseHealingWellsAt,currentHealthPercent)) != NULL)
						{
							strcpy(D3Mail.D3Info.retUnit.Name, Shrine_ACD->Name);
							if(sqrt(pow(Shrine_ACD->X-MyACD::GetMyACD()->X,2)+pow(Shrine_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
							{
								UsePower(30021,Shrine_ACD);	
							}
							else
							{
								UsePower(NULL, NULL, Shrine_ACD->X,Shrine_ACD->Y);
							}
							D3Mail.r_1.i = 1;
						}
						break;
					case D3_UseWaypoint:
						UseWaypoint(D3Mail.p_1.i);
						break;
					case D3_PickUpMoney:
						ACDActors::ACDActor * Money_Actor;
						srand (time(NULL)); 
						D3Mail.r_1.i = 0;
						if((Money_Actor = MyACD::GetNearestACDMoney(D3Mail.D3Info.Settings.MoneyMinAmount)) != NULL)
						{
							D3Mail.r_2.i = ACDActors::GetAttributByString(Money_Actor, "Gold");
							UsePower(NULL, NULL, Money_Actor->X+(rand()%8)-4,Money_Actor->Y+(rand()%8)-4);
							D3Mail.r_1.i = 1;
						}
						break;
					case D3_SelectQuestStart:
						SelectQuestClass::SetMonsterLevel(D3Mail.p_5.i);
						SelectQuestClass::SetQuest(D3Mail.p_4.i, D3Mail.p_1.i, D3Mail.p_2.i, D3Mail.p_3.i,0);
						MyACD::EnterWorld();
						break;
					case D3_SelectQuestResume:
						SelectQuestClass::SetMonsterLevel(D3Mail.p_5.i);
						SelectQuestClass::SetQuest(D3Mail.p_4.i, D3Mail.p_1.i, D3Mail.p_2.i, D3Mail.p_3.i,1);
						MyACD::EnterWorld();
						break;
					case D3_AttackMonster:
						D3Mail.r_1.i = 0;
						ACDActors::ACDActor *SaveActor;
						SaveActor = MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange);
						if(SaveActor != NULL && MyACD::GetMyACD() != NULL)
						{	
							int cSkillID;
							static int* skillList;
							//printf("skillcount%d\n",skillcount);
							if(skillList == NULL) {
								skillList = new int[skillcount];
								//printf("b4 priority list\n");
								MyACD::GetSkillPriorityList(skillList,skillcount,D3Mail.D3Info.PassedSpellRulePath,D3Mail.D3Info.Settings.SpellRulesEnabled);
									//printf("after priority list\n");
							}
							for(int i=0;i<skillcount;++i)// iterate through the array
							{
								cSkillID = skillList[i];
								if(cSkillID <= 0) {
									//skillList[i] = NULL;
									//printf("skip %d\n",SkillID);
									continue;
								}
								//code to check all the normal "is it ready,can i cast" but then check the qualifications in the array.
								if((SaveActor = MyACD::GetNearestAttackableActor(D3Mail.D3Info.Settings.AttackRange)) != NULL && cSkillID > 0)
								{
									//printf("iteration:%d\n",iteration);
									if((iteration%2) == 0 && MyACD::checkSpellRules(cSkillID,currentHealthPercent,D3Mail.D3Info.resource,D3Mail.D3Info.resource2,skillcount,D3Mail.D3Info.PassedSpellRulePath,D3Mail.D3Info.Settings.SpellRulesEnabled)){
										UsePower(cSkillID, SaveActor, -1, -1, SaveActor->id_acd);
										UsePower(cSkillID, SaveActor);
									} else {
										if((iteration%2) == 1) UsePower(cSkillID, SaveActor,SaveActor->X,SaveActor->Y,SaveActor->id_acd);
									}
									//UsePowerToPosition(cSkillID,SaveActor->X, SaveActor->Y,SaveActor->Z);
									strcpy(D3Mail.D3Info.retUnit.Name, SaveActor->Name);
								}
								//skillList[i] = NULL;
							}
							//printf("\nout of loop\n");
							/*if(GetAsyncKeyState(VK_F4) < 0){
							while(true){
								Sleep(1000);
							}
							}*/
							D3Mail.r_1.i = 1;
							//delete[] skillList;
							//skillList = NULL;
						}
						break;
					case D3_CastSpellToUnstuck:
						if(D3Mail.p_1.i >=0 && D3Mail.p_1.i < 6)
							UsePowerToPosition(MyACD::GetSkillID(D3Mail.p_1.i), MyACD::GetMyACD()->X, MyACD::GetMyACD()->Y, MyACD::GetMyACD()->Z);
						break;
					case D3_SkipScene:
						//printf("skipping scene\n");
						MyACD::SkipScene();
						break;
					case D3_Revival:
						MyACD::Revival();
						break;
					case D3_LeaveWorld:
						MyACD::LeaveWorld();
						break;
					case D3_Stash:
						D3Mail.r_1.i = MyACD::StashItems(D3Mail.D3Info.PotionStacksAllowed,D3Mail.D3Info.SellPotions);
						break;
					case D3_Repair:
							strcpy((char*)*((DWORD*)(pRepair+0xC6)),"Root.NormalLayer.shop_dialog_mainPage.repair_dialog");
							MyACD::Repair(D3Mail.p_1.i);
						break;
					case D3_Login:
						//printf("login called from gui.\n");
						D3Mail.r_11.i = 0;
						D3Mail.r_1.i = UIElements::Login(D3Mail.StringEmail, D3Mail.StringPassword);
						break;
					case D3_UsePowerToActor:
						ACDActors::ACDActor * UsePower_ACD;

						if((UsePower_ACD = ACDActors::GetACDActorByGUID(D3Mail.p_1.i)) != NULL)
						{
							switch(UsePower_ACD->id_sno)
							{
								case 0x26765/*trDun_Blacksmith_CellarDoor_Breakable*/:
								case 0x2FA1D/*Barricade_Breakable_Snow_A*/:
								case 0x2FA24/*Barricade_Doube_Breakable_Snow_A*/:
								case 0x2F5EE/*a3dun_Keep_Cart_A_Breakable_charred*/:
								case 0x2F5E4/*a3dun_Keep_Barrel_B_Breakable_charred*/:
								case 0x26D5A/*a3dun_Bridge_Barricade_A*/:
								case 0x2F5AB/*a3_Battlefield_Barricade_Breakable_charred*/:
								case 0x2F58C/*a3_Battlefield_Barricade_Double_Breakable_charred*/:	
								case 0x1BD0C://	Trout_Log 1BD0C
								case 0x1C71C://	trOut_Log_Highlands 1C71C
								case 0xFB0F://	trOut_Stump_Chest FB0F
								//case 0x14B06://	trOut_Wilderness_Grave_Chest 14B06
								case 0x30151://	a2dun_Spider_EggSack__Chest 30151
								case 0x1833F://	a1dun_Leor_Body_Tumbler 1833F
								case 0x18270://	a1dun_Leor_Large_Rack 18270
								case 0x16971://	caOut_Breakable_Wagon_b 16971
								case 0x16C7A://	caOut_Breakable_Wagon_C 16C7A
								case 0x15F2://	ToolBoxA_caOut_Props 15F2
								case 0x15F4://	ToolBoxSetA_caOut_Props 15F4
								case 0x36476://	a2dun_Zolt_Book_Holder_A 36476
								case 0x3038A://	a2dun_Zolt_Random_Breakable_Table_Sand 3038A
								case 0xF2AD://	a2dun_Aqd_Act_Wood_Platform_A_01 F2AD
								case 0xE087://	caOut_BoneYards_Collapsing_Bones E087
								case 0x1A82B://	a3dun_Keep_Crate_B_Snow 1A82B
								case 0x29AA1://	A3_Battlefield_Cart_A_Breakable 29AA1
								case 0x1C6B9://	a3dun_Bridge_Munitions_Cart_A 1C6B9
								case 0xCE3E://	a3dun_Keep_Crate_B CE3E
								case 0x1DAF2://	A3_Battlefield_Wagon_SupplyCart_A_Breakable 1DAF2
								case 0x34351://	a3Battlefield_Props_burnt_supply_wagon_Breakable_A 34351
								case 0x3457C://	a3Battlefield_Props_burnt_supply_wagon_B_Breakable 3457C
								case 0x2F5DF://	a3dun_Keep_Crate_B_charred 2F5DF
								case 0x2A9A0://	A3_crater_st_DemonCage_A 2A9A0
								case 0x1656://	trDun_Cath_Barricade_A 1656
								case 0x1657://	trDun_Cath_Barricade_B 1657
								case 0x2C3EC://	caOut_StingingWinds_Barricade_A 2C3EC
								case 0x1A6A2://	a2dunSwr_Breakables_Barricade_B 1A6A2
								case 0xF4BD://	a2dun_Aqd_Act_Barricade_A_01 F4BD
								case 0x26F49://	a3dun_Bridge_Barricade_C 26F49
								case 0x2733A://	a3dun_Bridge_Barricade_D 2733A
								case 0x16A0://	trDun_Cath_WoodDoor_A_Barricaded 16A0
								case 0x16BF://	trDun_Crypt_Door 16BF
								case 0x19394://	TrOut_Highlands_Manor_Front_Gate 19394 
								case 0x174F9://	a1dun_Leor_Jail_Door_Breakable_A 174F9
								case 0xD81D://	a3dun_Keep_Door_Destructable D81D
								case 0x22947://	a3dun_Keep_Exploding_Arch_A 22947
									UsePower(MyACD::GetSkillID((D3Mail.D3Info.Settings.UnstuckSpell >= 0 && D3Mail.D3Info.Settings.UnstuckSpell < 6) ? D3Mail.D3Info.Settings.UnstuckSpell : 0),UsePower_ACD);
									break;
								default:
									UsePower(((ACDActors::isGizmo(UsePower_ACD))?30021:30022),UsePower_ACD);
									break;
							}
							if(sqrt(pow(UsePower_ACD->X-MyACD::GetMyACD()->X,2)+pow(UsePower_ACD->Y-MyACD::GetMyACD()->Y,2)) <= 10)
								D3Mail.r_1.i = D3Mail.p_2.i + 1;
							else
								D3Mail.r_1.i = 1;
							if(ACDActors::isGizmo(UsePower_ACD) && ACDActors::GetAttributByString(UsePower_ACD,"Gizmo_Has_Been_Operated") == 1)
								D3Mail.r_1.i = -1;
						}
						else
							D3Mail.r_1.i = -1;
						break;
					case D3_MoveTo:
						if(MyACD::GetMyACD() != NULL){
						int speedSkillIndex=0;
						while(speedSkillIndex<=skillcount){
							if(speedSkillIndex == skillcount) break;
							if(MyACD::checkSpellRules(MyACD::GetSkillID(speedSkillIndex),currentHealthPercent,D3Mail.D3Info.resource,D3Mail.D3Info.resource2,skillcount,D3Mail.D3Info.PassedSpellRulePath,D3Mail.D3Info.Settings.SpellRulesEnabled,true)) break;
							++speedSkillIndex;
						}
						if(speedSkillIndex < skillcount) {
							UsePowerToPosition(MyACD::GetSkillID(speedSkillIndex),D3Mail.p_1.f,D3Mail.p_2.f,MyACD::GetMyACD()->Z);
						}

						UsePower(NULL, NULL, D3Mail.p_1.f,D3Mail.p_2.f);
						}
						break;
					case D3_UseTownPortal:
							if(!D3Mail.D3Info.inTown)
							{
								if(!MyACD::SkillAktive(0x0002EC66))	{
									MyACD::useTownportal();
								}
								D3Mail.r_1.i = 0;
							}
							else
							{
								D3Mail.r_1.i = 1;
							}
						break;
				}
				GraphicFlag = D3Mail.D3Info.GraphicFlag;
				if(D3Mail.r_11.i == 1) {
					MyACD::SkipScene();
					if(UIElements::GetVisibility("Root.TopLayer.confirmation.subdlg.stack.message") && strstr(UIElements::GetUI("Root.TopLayer.confirmation.subdlg.stack.message")->pntrText,"skip")){
						SendRandomlyTimedEnter(); 
						D3Mail.r_11.i = 0;
					}
				}
				//printf("R_11:%d\n",D3Mail.r_11.i);
			}__except(exFilter(GetExceptionCode(), GetExceptionInformation(),D3Mail.header)){}
		}
	WriteFile(hPipe,&D3Mail,sizeof(D3Mail),&cbWritten,NULL);
	FlushFileBuffers(hPipe);
	DisconnectNamedPipe(hPipe);
	CloseHandle(hPipe);
	iteration++;
	if(iteration > 19) iteration = 0;
}

void ListenPipe(LPVOID HandlePipe){
	char lpszPipename[30] = {0}; 
	HANDLE hPipe = NULL;
	BOOL   fConnected = FALSE;
	sprintf_s(lpszPipename,"\\\\.\\pipe\\D3_%i",GetCurrentProcessId()); 
	while(true)
	{
		//printf("listen pipe\n");
		hPipe = CreateNamedPipeA(lpszPipename,
					PIPE_ACCESS_DUPLEX,PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
					PIPE_UNLIMITED_INSTANCES,
					sizeof(D3Mail),sizeof(D3Mail),200,NULL);
		fConnected = ConnectNamedPipe(hPipe, NULL) ? TRUE : (GetLastError() == ERROR_PIPE_CONNECTED); 
			if(fConnected){
				//printf("connected\n");
				D3MailQueueHandle.push(hPipe);
			} else {
				CloseHandle(hPipe);
			}
		Sleep(10);
	}
}


void _declspec(naked) MainTLS()
{	
	_asm pushad;
	static DWORD retOriginalFunc = (DWORD)(TLSHook->HookLocation + TLSHook->Size);

	InstanceThread();
	if(GraphicFlag==1)
	{
		*(DWORD*)pGetgameWindowState = 0;	
		ShowWindow(((GetHWND)pGetHWND)() , SW_HIDE);
		Sleep(20);
	}
	else
	{
		ShowWindow(((GetHWND)pGetHWND)() , SW_SHOW);
	}
	_asm popad

	if(GraphicFlag == 1)
		_asm mov eax, 0
	else 
		_asm CALL GraphicCall

	_asm jmp retOriginalFunc
}
