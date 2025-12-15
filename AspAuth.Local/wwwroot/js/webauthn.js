async function createCredential(headers, signal) {
  // Step 2: Request creation options from the server
  const optionsResponse = 
    await fetchWithErrorHandling('/Account/PasskeyCreationOptions', 
    {
      method: 'POST',
      headers,
      signal,
    });
  const optionsJson = await optionsResponse.json();
  const options = PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson);
  return await navigator.credentials.create({ publicKey: options, signal });
}