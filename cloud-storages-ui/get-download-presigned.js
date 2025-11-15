import config from "./config.js";

document.addEventListener("DOMContentLoaded", () => {
  const objectKeyInput = document.getElementById("objectKeyInput");
  const getDownloadUrlButton = document.getElementById("getDownloadUrlButton");
  const clearButton = document.getElementById("clearButton");
  const downloadPresignedUrlResult = document.getElementById(
    "downloadPresignedUrlResult"
  );

  getDownloadUrlButton.addEventListener("click", async () => {
    const objectKey = objectKeyInput.value.trim();
    if (!objectKey) {
      alert("Please enter an object key");
      return;
    }

    try {
      const downloadUrl = await getDownloadPresignedUrl(objectKey);
      displayDownloadPresignedUrl(downloadUrl);
    } catch (error) {
      console.error("Error:", error);
      alert("An error occurred while fetching the download presigned URL");
    }
  });

  clearButton.addEventListener("click", () => {
    objectKeyInput.value = "";
    downloadPresignedUrlResult.innerHTML = "";
    downloadPresignedUrlResult.classList.add("hidden");
  });

  async function getDownloadPresignedUrl(key) {
    const response = await fetch(
      `${config.API_BASE_URL}/${encodeURIComponent(key)}/download-url`
    );
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    const data = await response.json();
    return data.downloadUrl;
  }

  function displayDownloadPresignedUrl(downloadUrl) {
    // Create a new anchor element
    const link = document.createElement("a");
    link.href = downloadUrl;
    link.target = "_blank";
    link.rel = "noopener noreferrer";

    // Clip the URL
    const maxLength = 50; // Adjust this value to change the clipping length
    const clippedUrl =
      downloadUrl.length > maxLength
        ? downloadUrl.substring(0, maxLength - 3) + "..."
        : downloadUrl;
    link.textContent = clippedUrl;

    // Set the full URL as a title (tooltip)
    link.title = downloadUrl;

    // Clear any existing content and add the new link
    downloadPresignedUrlResult.innerHTML = "";
    downloadPresignedUrlResult.appendChild(link);
    downloadPresignedUrlResult.classList.remove("hidden");

    // Add Tailwind classes for styling
    link.className = "text-blue-600 hover:text-blue-800 underline";
  }
});
