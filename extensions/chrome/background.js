const nativeHostName = "com.woong.monitorstack.chrome";
let nativePort = null;
let reconnectTimer = null;

async function detectBrowserFamily() {
  const userAgent = navigator.userAgent || "";
  if (userAgent.includes("Edg/")) {
    return "Microsoft Edge";
  }

  if (navigator.brave && await navigator.brave.isBrave()) {
    return "Brave";
  }

  return "Chrome";
}

function getNativePort() {
  if (nativePort) {
    return nativePort;
  }

  nativePort = chrome.runtime.connectNative(nativeHostName);
  nativePort.onDisconnect.addListener(() => {
    nativePort = null;
    scheduleReconnectReport();
  });
  return nativePort;
}

function scheduleReconnectReport() {
  if (reconnectTimer) {
    return;
  }

  reconnectTimer = setTimeout(async () => {
    reconnectTimer = null;
    await reportCurrentActiveTab();
  }, 5000);
}

async function sendActiveTab(tab) {
  if (!tab || !tab.id || !tab.url || !tab.url.startsWith("http")) {
    return;
  }

  const message = {
    type: "activeTabChanged",
    browserFamily: await detectBrowserFamily(),
    windowId: tab.windowId,
    tabId: tab.id,
    url: tab.url,
    title: tab.title || "",
    observedAtUtc: new Date().toISOString()
  };

  try {
    const port = getNativePort();
    port.postMessage(message);
  } catch {
    // The Windows app may be closed or the native host may not be registered yet.
    nativePort = null;
    scheduleReconnectReport();
  }
}

async function reportCurrentActiveTab() {
  const tabs = await chrome.tabs.query({ active: true, currentWindow: true });
  if (tabs.length > 0) {
    await sendActiveTab(tabs[0]);
  }
}

chrome.runtime.onStartup.addListener(reportCurrentActiveTab);
chrome.runtime.onInstalled.addListener(reportCurrentActiveTab);

chrome.tabs.onActivated.addListener(async activeInfo => {
  const tab = await chrome.tabs.get(activeInfo.tabId);
  await sendActiveTab(tab);
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === "complete" || changeInfo.url || changeInfo.title) {
    await sendActiveTab(tab);
  }
});

reportCurrentActiveTab();
