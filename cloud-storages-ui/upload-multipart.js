import config from "./config.js";

document.addEventListener("DOMContentLoaded", () => {
  const fileInputMultipart = document.getElementById("fileInputMultipart");
  const uploadButtonMultipart = document.getElementById(
    "uploadButtonMultipart"
  );
  const pauseResumeButton = document.getElementById("pauseResumeButton");
  const uploadProgressMultipart = document.getElementById(
    "uploadProgressMultipart"
  );
  const progressBarMultipart = document.getElementById("progressBarMultipart");
  const progressTextMultipart = document.getElementById(
    "progressTextMultipart"
  );
  const multipartSidebar = document.getElementById("multipartSidebar");
  const partProgressList = document.getElementById("partProgressList");
  const uploadedFileKey = document.getElementById("uploadedFileKey");

  let uploadState = {
    file: null,
    key: null,
    uploadId: null,
    parts: [],
    currentPart: 0,
    uploadedParts: [],
    isPaused: false,
  };

  uploadButtonMultipart.addEventListener("click", startUpload);
  pauseResumeButton.addEventListener("click", togglePauseResume);

  async function startUpload() {
    const file = fileInputMultipart.files[0];
    if (!file) {
      alert("Please select a file");
      return;
    }

    uploadState.file = file;
    uploadState.currentPart = 0;
    uploadState.uploadedParts = [];
    uploadState.isPaused = false;

    try {
      // Show progress bar and sidebar
      uploadProgressMultipart.classList.remove("hidden");
      multipartSidebar.classList.remove("translate-x-full");
      pauseResumeButton.classList.remove("hidden");
      pauseResumeButton.textContent = "Pause";

      // Clear previous part progress
      partProgressList.innerHTML = "";

      // Initiate multipart upload
      const { uploadId, key } = await initiatePartsUpload(file.name, file.type);
      uploadState.uploadId = uploadId;
      uploadState.key = key;

      // Prepare parts
      const partSize = 100 * 1024 * 1024; // 100 MB converted to bytes per part
      uploadState.parts = prepareParts(file, partSize);

      // Start uploading parts
      await uploadParts();
    } catch (error) {
      console.error("Error:", error);
      alert("An error occurred while uploading the file");
    }
  }

  function prepareParts(file, partSize) {
    const parts = [];
    for (let i = 0; i < file.size; i += partSize) {
      parts.push({
        start: i,
        end: Math.min(i + partSize, file.size),
      });
    }
    return parts;
  }
  // [
  //   { start: 0, end: 104857600 },          //Phần 1: 0 → 100MB
  //   { start: 104857600, end: 209715200 }, // Phần 2: 100MB → 200MB
  //   { start: 209715200, end: 262144000 }  // Phần 3: 200MB → 250MB
  // ]

  async function uploadPart(preSignedUrl, partFile) {
    return fetch(preSignedUrl, {
      method: "PUT",
      body: partFile,
    });
  }
  async function uploadParts() {
    while (
      uploadState.currentPart < uploadState.parts.length &&
      !uploadState.isPaused
    ) {
      const part = uploadState.parts[uploadState.currentPart];

      // Slice the part from the file
      const partFile = uploadState.file.slice(part.start, part.end);

      // Create progress element for this part and add it to the top of the list
      const partProgressElement = createPartProgressElement(
        uploadState.currentPart + 1
      );
      partProgressList.insertBefore(
        partProgressElement,
        partProgressList.firstChild
      );

      try {
        const { preSignedUrl } = await getUploadPartPreSignedUrl(
          uploadState.key,
          uploadState.uploadId,
          uploadState.currentPart + 1
        );
        const uploadPartResponse = await uploadPart(preSignedUrl, partFile);

        if (uploadPartResponse.ok) {
          const etag = uploadPartResponse.headers.get("ETag");
          uploadState.uploadedParts.push({
            PartNumber: uploadState.currentPart + 1,
            ETag: etag,
          });
          updatePartProgress(partProgressElement, 100);
        } else {
          updatePartProgress(partProgressElement, 0, "Failed");
        }
      } catch (error) {
        console.error(
          `Error uploading part ${uploadState.currentPart + 1}:`,
          error
        );
        updatePartProgress(partProgressElement, 0, "Failed");
      }

      uploadState.currentPart++;
      updateOverallProgress();
    }

    if (
      uploadState.currentPart === uploadState.parts.length &&
      !uploadState.isPaused
    ) {
      const result = await completePartsUpload(
        uploadState.key,
        uploadState.uploadId,
        uploadState.uploadedParts
      );
      displayUploadedFileKey(result.key);
      alert("File uploaded successfully using multipart upload!");
      resetUploadState();
    }
  }

  function togglePauseResume() {
    uploadState.isPaused = !uploadState.isPaused;
    pauseResumeButton.textContent = uploadState.isPaused ? "Resume" : "Pause";
    if (!uploadState.isPaused) {
      uploadParts();
    }
  }

  function updateOverallProgress() {
    const percentComplete =
      (uploadState.currentPart / uploadState.parts.length) * 100;
    progressBarMultipart.style.width = percentComplete + "%";
    progressTextMultipart.textContent = percentComplete.toFixed(2) + "%";
  }

  function resetUploadState() {
    uploadState = {
      file: null,
      key: null,
      uploadId: null,
      parts: [],
      currentPart: 0,
      uploadedParts: [],
      isPaused: false,
    };
    pauseResumeButton.classList.add("hidden");
    uploadProgressMultipart.classList.add("hidden");
    multipartSidebar.classList.add("translate-x-full");
    progressBarMultipart.style.width = "0%";
    progressTextMultipart.textContent = "0%";
  }

  async function initiatePartsUpload(fileName, contentType) {
    const response = await fetch(
      `${config.API_BASE_URL}/initiate-parts-upload`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName, contentType }),
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return await response.json();
  }

  async function getUploadPartPreSignedUrl(key, uploadId, partNumber) {
    const response = await fetch(
      `${config.API_BASE_URL}/${encodeURIComponent(
        key
      )}/upload-part-presigned-url`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ uploadId, partNumber }),
      }
    );

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  async function completePartsUpload(key, uploadId, parts) {
    const response = await fetch(
      `${config.API_BASE_URL}/${encodeURIComponent(key)}/complete-parts-upload`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ uploadId, parts }),
      }
    );
    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }
    return await response.json();
  }

  function createPartProgressElement(partNumber) {
    const div = document.createElement("div");
    div.className = "mb-2 part-progress";
    div.innerHTML = `
      <p class="text-sm font-semibold">Part ${partNumber}</p>
      <div class="bg-gray-200 rounded-full h-2">
        <div class="bg-blue-600 h-2 rounded-full" style="width: 0%"></div>
      </div>
      <p class="text-xs mt-1">0%</p>
    `;
    return div;
  }

  function updatePartProgress(element, percent, status = "") {
    const progressBar = element.querySelector(".bg-blue-600");
    const progressText = element.querySelector("p:last-child");
    progressBar.style.width = `${percent}%`;
    progressText.textContent = status || `${percent.toFixed(2)}%`;
  }

  function displayUploadedFileKey(key) {
    uploadedFileKey.textContent = `Uploaded file key: ${key}`;
    uploadedFileKey.classList.remove("hidden");
  }
});
