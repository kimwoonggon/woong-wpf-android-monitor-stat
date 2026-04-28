const nativeHostName = "com.woong.monitorstack.chrome";
const browserFamily = "Chrome";

async function sendActiveTab(tab) {
  if (!tab || !tab.id || !tab.url || !tab.url.startsWith("http")) {
    return;
  }

  const message = {
    type: "activeTabChanged",
    browserFamily,
    windowId: tab.windowId,
    tabId: tab.id,
    url: tab.url,
    title: tab.title || "",
    observedAtUtc: new Date().toISOString()
  };

  try {
    await chrome.runtime.sendNativeMessage(nativeHostName, message);
  } catch {
    // The Windows app may be closed or the native host may not be registered yet.
  }
}

chrome.tabs.onActivated.addListener(async activeInfo => {
  const tab = await chrome.tabs.get(activeInfo.tabId);
  await sendActiveTab(tab);
});

chrome.tabs.onUpdated.addListener(async (tabId, changeInfo, tab) => {
  if (changeInfo.status === "complete" || changeInfo.url || changeInfo.title) {
    await sendActiveTab(tab);
  }
});
