#pragma once
#include <Windows.h>
#define SystemProcessInformation	5
#define STATUS_INFO_LENGTH_MISMATCH	0xC0000004
#define NT_SUCCESS(Status)			((NTSTATUS)(Status) >= 0)

#define UNLINK(x) (x).Blink->Flink = (x).Flink; (x).Flink->Blink = (x).Blink;

struct UNICODE_STRING
{
	USHORT Length;
	USHORT MaximumLength;
	PWSTR Buffer;
};

struct ModuleInfoNode
{
	LIST_ENTRY LoadOrder;
	LIST_ENTRY InitOrder;
	LIST_ENTRY MemoryOrder;
	HMODULE BaseAddress;
	unsigned long EntryPoint;
	unsigned int Size;
	UNICODE_STRING FullPath;
	UNICODE_STRING Name;
	unsigned long Flags;
	unsigned short LoadCount;
	unsigned short TlsIndex;
	LIST_ENTRY HashTable;
	unsigned long TimeStamp;
};

struct ProcessModuleInfo
{
	unsigned int Size;
	unsigned int Initialized;
	HANDLE SsHandle;
	LIST_ENTRY LoadOrder;
	LIST_ENTRY InitOrder;
	LIST_ENTRY MemoryOrder;
};

typedef struct _SClientId
{
	DWORD	UniqueProcess;
	DWORD	UniqueThread;
} SClientId, *PSClientId;

typedef struct _SUnicodeString
{
	USHORT	Length;
	USHORT	MaximumLength;
	PWSTR	Buffer;
} SUnicodeString, *PSUnicodeString;

typedef struct _SVMCounters
{
	SIZE_T	PeakVirtualSize;
	SIZE_T	VirtualSize;
	ULONG	PageFaultCount;
	SIZE_T	PeakWorkingSetSize;
	SIZE_T	WorkingSetSize;
	SIZE_T	QuotaPeakPagedPoolUsage;
	SIZE_T	QuotaPagedPoolUsage;
	SIZE_T	QuotaPeakNonPagedPoolUsage;
	SIZE_T	QuotaNonPagedPoolUsage;
	SIZE_T	PagefileUsage;
	SIZE_T	PeakPagefileUsage;
	BYTE	Reserved[48];
} SVMCounters, *PSVMCounters;

typedef struct _SSystemThreads
{
	LARGE_INTEGER	KernelTime;
	LARGE_INTEGER	UserTime;
	LARGE_INTEGER	CreateTime;
	ULONG			WaitTime;
	PVOID			StartAddress;
	SClientId		ClientId;
	LONG			Priority;
	LONG			BasePriority;
	ULONG			ContextSwitchCount;
	LONG			State;
	LONG			WaitReason;
} SSystemThreads, *PSSystemThreads;

typedef struct _SSystemProcess
{
	ULONG			NextEntryDelta;
	ULONG			ThreadCount;
	ULONG			Reserved1[6];
	LARGE_INTEGER	CreateTime;
	LARGE_INTEGER	UserTime;
	LARGE_INTEGER	KernelTime;
	SUnicodeString	ProcessName;
	LONG			BasePriority;
	ULONG			ProcessId;
	ULONG			InheritedFromProcessId;
	ULONG			HandleCount;
	ULONG			Reserved2[2];
	SVMCounters		VmCounters;
	SSystemThreads	sThreads[1];
} SSystemProcess, *PSSystemProcess;

typedef struct _SObjectAttributes
{
	ULONG			Length;    
	HANDLE			RootDirectory;    
	PSUnicodeString	ObjectName;    
	ULONG			Attributes;    
	PVOID			SecurityDescriptor;    
	PVOID			SecurityQualityOfService;
} SObjectAttributes, *PSObjectAttributes;  

unsigned int CloakModule(HMODULE hMod)
{
	ProcessModuleInfo* pmInfo;
	ModuleInfoNode* module;
	unsigned int moduleSize = 0;

	_asm {
		MOV EAX,DWORD PTR FS:[0x18]
		MOV EAX,DWORD PTR DS:[EAX+0x30]
		MOV EAX,DWORD PTR DS:[EAX+0xC]
		MOV pmInfo,EAX
	}

	module = (ModuleInfoNode*)(pmInfo->LoadOrder.Flink);

	while(module->BaseAddress && module->BaseAddress != hMod)
		module = (ModuleInfoNode*)(module->LoadOrder.Flink);

	if(!module->BaseAddress)
		return moduleSize;

	UNLINK(module->LoadOrder);
	UNLINK(module->InitOrder);
	UNLINK(module->MemoryOrder);
	UNLINK(module->HashTable);

	memset(module->FullPath.Buffer, 0, module->FullPath.Length);

	moduleSize = module->Size;

	DWORD dwOldProtection;
	VirtualProtect(module->BaseAddress, 0x1000, PAGE_EXECUTE_READWRITE, &dwOldProtection);
	memset((void*)module->BaseAddress, 0, 0x1000);
	VirtualProtect(module->BaseAddress, 0x1000, dwOldProtection, NULL);

	memset(module, 0, sizeof(ModuleInfoNode));

	return moduleSize;
}