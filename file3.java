/*
 * oauth2-oidc-sdk
 *
 * Copyright 2012-2016, Connect2id Ltd and contributors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use
 * this file except in compliance with the License. You may obtain a copy of the
 * License at
 *
 *    http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed
 * under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
 * CONDITIONS OF ANY KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations under the License.
 */

package com.nimbusds.openid.connect.sdk.rp;


import java.net.URI;
import java.util.Collections;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

import junit.framework.TestCase;

import com.nimbusds.common.contenttype.ContentType;
import com.nimbusds.jose.EncryptionMethod;
import com.nimbusds.jose.JWEAlgorithm;
import com.nimbusds.jose.JWSAlgorithm;
import com.nimbusds.jose.JWSHeader;
import com.nimbusds.jose.crypto.MACSigner;
import com.nimbusds.jose.crypto.MACVerifier;
import com.nimbusds.jwt.JWTClaimsSet;
import com.nimbusds.jwt.SignedJWT;
import com.nimbusds.langtag.LangTag;
import com.nimbusds.oauth2.sdk.GrantType;
import com.nimbusds.oauth2.sdk.auth.ClientAuthenticationMethod;
import com.nimbusds.oauth2.sdk.client.*;
import com.nimbusds.oauth2.sdk.http.HTTPRequest;
import com.nimbusds.oauth2.sdk.http.HTTPResponse;
import com.nimbusds.oauth2.sdk.token.BearerAccessToken;
import com.nimbusds.openid.connect.sdk.SubjectType;



/**
 * Tests the OIDC client registration class.
 */
public class OIDCClientRegistrationRequestTest extends TestCase {
	
	
	public void testRoundtrip() throws Exception {
		
		URI uri = new URI("https://server.example.com/connect/register");
		
		OIDCClientMetadata metadata = new OIDCClientMetadata();
		
		Set<URI> redirectURIs = new HashSet<>();
		redirectURIs.add(new URI("https://client.example.org/callback"));
		metadata.setRedirectionURIs(redirectURIs);
		
		metadata.setApplicationType(ApplicationType.NATIVE);
		
		metadata.setJWKSetURI(new URI("https://client.example.org/my_public_keys.jwks"));
		
		OIDCClientRegistrationRequest request = new OIDCClientRegistrationRequest(uri, metadata, null);
		
		assertEquals(uri, request.getEndpointURI());
		
		assertNull(request.getAccessToken());
		
		metadata = request.getOIDCClientMetadata();
		
		redirectURIs = metadata.getRedirectionURIs();
		assertTrue(redirectURIs.contains(new URI("https://client.example.org/callback")));
		assertEquals(1, redirectURIs.size());
		
		assertEquals(ApplicationType.NATIVE, metadata.getApplicationType());
		
		assertEquals(new URI("https://client.example.org/my_public_keys.jwks"), metadata.getJWKSetURI());
		
		HTTPRequest httpRequest = request.toHTTPRequest();
		
		assertEquals(HTTPRequest.Method.POST, httpRequest.getMethod());
		assertEquals(ContentType.APPLICATION_JSON.toString(), httpRequest.getEntityContentType().toString());
		
		System.out.println(httpRequest.getQuery());
		
		request = OIDCClientRegistrationRequest.parse(httpRequest);
		
		assertEquals(uri, request.getEndpointURI());
		
		assertNull(request.getAccessToken());
		
		metadata = request.getOIDCClientMetadata();
		
		redirectURIs = metadata.getRedirectionURIs();
		assertTrue(redirectURIs.contains(new URI("https://client.example.org/callback")));
		assertEquals(1, redirectURIs.size());
		
		assertEquals(ApplicationType.NATIVE, metadata.getApplicationType());
		
		assertEquals(new URI("https://client.example.org/my_public_keys.jwks"), metadata.getJWKSetURI());
	}
		
	
	public void testParse() throws Exception {
		
		URI uri = new URI("https://server.example.com/connect/register");
		
		String json = "{"
			+ "   \"application_type\": \"web\","
			+ "   \"redirect_uris\":[\"https://client.example.org/callback\",\"https://client.example.org/callback2\"],"
			+ "   \"client_name\": \"My Example\","
			+ "   \"client_name#ja-Jpan-JP\":\"クライアント名\","
			+ "   \"logo_uri\": \"https://client.example.org/logo.png\","
			+ "   \"subject_type\": \"pairwise\","
			+ "   \"sector_identifier_uri\":\"https://other.example.net/file_of_redirect_uris.json\","
			+ "   \"token_endpoint_auth_method\": \"client_secret_basic\","
			+ "   \"jwks_uri\": \"https://client.example.org/my_public_keys.jwks\","
			+ "   \"userinfo_encrypted_response_alg\": \"RSA1_5\","
			+ "   \"userinfo_encrypted_response_enc\": \"A128CBC-HS256\","
			+ "   \"contacts\": [\"ve7jtb@example.org\", \"mary@example.org\"],"
			+ "   \"request_uris\":[\"https://client.example.org/rf.txt#qpXaRLh_n93TTR9F252ValdatUQvQiJi5BDub2BeznA\"]"
			+ "  }";
		
		System.out.println(json);
		
		HTTPRequest httpRequest = new HTTPRequest(HTTPRequest.Method.POST, uri.toURL());
		httpRequest.setAuthorization("Bearer eyJhbGciOiJSUzI1NiJ9.eyJ");
		httpRequest.setEntityContentType(ContentType.APPLICATION_JSON);
		httpRequest.setQuery(json);
		
		OIDCClientRegistrationRequest req = OIDCClientRegistrationRequest.parse(httpRequest);
		
		assertEquals(uri, req.getEndpointURI());
		
		OIDCClientMetadata metadata = req.getOIDCClientMetadata();
		
		assertEquals(ApplicationType.WEB, metadata.getApplicationType());
		
		Set<URI> redirectURIs = metadata.getRedirectionURIs();
		assertTrue(redirectURIs.contains(new URI("https://client.example.org/callback")));
		assertTrue(redirectURIs.contains(new URI("https://client.example.org/callback2")));
		assertEquals(2, redirectURIs.size());
		
		assertEquals("My Example", metadata.getName());
		assertEquals("My Example", metadata.getName(null));
		assertEquals("クライアント名", metadata.getName(LangTag.parse("ja-Jpan-JP")));
		assertEquals(2, metadata.getNameEntries().size());
		
		assertEquals(new URI("https://client.example.org/logo.png"), metadata.getLogoURI());
		assertEquals(new URI("https://client.example.org/logo.png"), metadata.getLogoURI(null));
		assertEquals(1, metadata.getLogoURIEntries().size());
		
		assertEquals(SubjectType.PAIRWISE, metadata.getSubjectType());
		assertEquals(new URI("https://other.example.net/file_of_redirect_uris.json"), metadata.getSectorIDURI());
		
		assertEquals(ClientAuthenticationMethod.CLIENT_SECRET_BASIC, metadata.getTokenEndpointAuthMethod());
		
		assertEquals(new URI("https://client.example.org/my_public_keys.jwks"), metadata.getJWKSetURI());
		
		assertEquals(JWEAlgorithm.RSA1_5, metadata.getUserInfoJWEAlg());
		assertEquals(EncryptionMethod.A128CBC_HS256, metadata.getUserInfoJWEEnc());
		
		List<String> contacts = metadata.getEmailContacts();
		assertTrue(contacts.contains("ve7jtb@example.org"));
		assertTrue(contacts.contains("mary@example.org"));
		assertEquals(2, contacts.size());
		
		Set<URI> requestObjectURIs = metadata.getRequestObjectURIs();
		assertTrue(requestObjectURIs.contains(new URI("https://client.example.org/rf.txt#qpXaRLh_n93TTR9F252ValdatUQvQiJi5BDub2BeznA")));
	}


	public void testSoftwareStatement()
		throws Exception {

		JWTClaimsSet claimsSet = new JWTClaimsSet.Builder()
			.issuer("https://c2id.com")
			.build();

		SignedJWT jwt = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), claimsSet);
		jwt.sign(new MACSigner("01234567890123456789012345678901"));

		OIDCClientMetadata metadata = new OIDCClientMetadata();
		metadata.setRedirectionURI(new URI("https://client.com/in"));
		metadata.setName("Test App");

		OIDCClientRegistrationRequest request = new OIDCClientRegistrationRequest(new URI("https://c2id.com/reg"), metadata, jwt, null);

		assertEquals(metadata, request.getClientMetadata());
		assertEquals(jwt, request.getSoftwareStatement());
		assertNull(request.getAccessToken());

		HTTPRequest httpRequest = request.toHTTPRequest();

		request = OIDCClientRegistrationRequest.parse(httpRequest);

		assertEquals("https://client.com/in", request.getClientMetadata().getRedirectionURIs().iterator().next().toString());
		assertEquals("Test App", request.getClientMetadata().getName());
		assertEquals(jwt.serialize(), request.getSoftwareStatement().getParsedString());
		assertTrue(request.getSoftwareStatement().verify(new MACVerifier("01234567890123456789012345678901")));
	}


	public void testRejectUnsignedSoftwareStatement()
		throws Exception {

		JWTClaimsSet claimsSet = new JWTClaimsSet.Builder()
			.issuer("https://c2id.com")
			.build();

		OIDCClientMetadata metadata = new OIDCClientMetadata();
		metadata.setRedirectionURI(new URI("https://client.com/in"));
		metadata.setName("Test App");

		try {
			new OIDCClientRegistrationRequest(
				new URI("https://c2id.com/reg"),
				metadata,
				new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), claimsSet),
				null);

		} catch (IllegalArgumentException e) {

			// ok
			assertEquals("The software statement JWT must be signed", e.getMessage());
		}

	}


	public void testRejectSoftwareStatementWithoutIssuer()
		throws Exception {

		SignedJWT jwt = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), new JWTClaimsSet.Builder().build());
		jwt.sign(new MACSigner("01234567890123456789012345678901"));

		OIDCClientMetadata metadata = new OIDCClientMetadata();
		metadata.setRedirectionURI(new URI("https://client.com/in"));
		metadata.setName("Test App");

		try {
			new OIDCClientRegistrationRequest(
				new URI("https://c2id.com/reg"),
				metadata,
				jwt,
				null);

		} catch (IllegalArgumentException e) {

			// ok
			assertEquals("The software statement JWT must contain an 'iss' claim", e.getMessage());
		}
	}
	
	
	public void _testExampleRegisterForCodeGrant()
		throws Exception {
		
		// The client registration endpoint
		URI clientsEndpoint = new URI("https://demo.c2id.com/c2id/clients");
		
		// Master API token for the clients endpoint
		BearerAccessToken masterToken = new BearerAccessToken("ztucZS1ZyFKgh0tUEruUtiSTXhnexmd6");
		
		// We want to register a client for the code grant
		OIDCClientMetadata clientMetadata = new OIDCClientMetadata();
		clientMetadata.setGrantTypes(Collections.singleton(GrantType.AUTHORIZATION_CODE));
		clientMetadata.setRedirectionURI(URI.create("https://example.com/cb"));
		clientMetadata.setName("My Client App");
		
		OIDCClientRegistrationRequest regRequest = new OIDCClientRegistrationRequest(
			clientsEndpoint,
			clientMetadata,
			masterToken
		);
		
		HTTPResponse httpResponse = regRequest.toHTTPRequest().send();
		
		ClientRegistrationResponse regResponse = OIDCClientRegistrationResponseParser.parse(httpResponse);
		
		if (! regResponse.indicatesSuccess()) {
			// We have an error
			ClientRegistrationErrorResponse errorResponse = (ClientRegistrationErrorResponse)regResponse;
			System.err.println(errorResponse.getErrorObject());
			return;
		}
		
		// Successful registration
		OIDCClientInformationResponse successResponse = (OIDCClientInformationResponse)regResponse;
		
		OIDCClientInformation clientInfo = successResponse.getOIDCClientInformation();
		
		// The client credentials - store them:
		
		// The client_id
		System.out.println("Client ID: " + clientInfo.getID());
		
		// The client_secret
		System.out.println("Client secret: " + "wR9CKyqrkMh6k4LrVuj2WbOnPIbF4ONL0Ynmv8JG6nCj");
		
		// The client's registration resource
		System.out.println("Client registration URI: " + clientInfo.getRegistrationURI());
		
		// The token for accessing the client's registration (for update, etc)
		System.out.println("Client reg access token: " + "d26cd4053b4fee21d65f4588bc3e28ad54760e270d7e0b3c0ec8849740b26dbcfada2ad0c8ba37bd149bc70e2ee480c7d24cd73acefe751392a4c5e4");
		
		// Print the remaining client metadata
		System.out.println("Client metadata: " + clientInfo.getMetadata().toJSONObject());
		
		
		// Query
		ClientReadRequest readRequest = new ClientReadRequest(
			clientInfo.getRegistrationURI(),
			clientInfo.getRegistrationAccessToken()
		);
		
		httpResponse = readRequest.toHTTPRequest().send();
		
		regResponse = OIDCClientRegistrationResponseParser.parse(httpResponse);
		
		if (! regResponse.indicatesSuccess()) {
			// We have an error
			ClientRegistrationErrorResponse errorResponse = (ClientRegistrationErrorResponse)regResponse;
			System.err.println(errorResponse.getErrorObject());
			return;
		}
		
		// Success
		successResponse = (OIDCClientInformationResponse)regResponse;
		
		System.out.println("Client registration data: " + successResponse.getClientInformation().toJSONObject());
		
		
		// Update client name
		clientMetadata = clientInfo.getOIDCMetadata();
		clientMetadata.setName("My app has a new name");
		
		// Send request
		ClientUpdateRequest updateRequest = new ClientUpdateRequest(
			clientInfo.getRegistrationURI(),
			clientInfo.getID(),
			clientInfo.getRegistrationAccessToken(),
			clientMetadata,
			clientInfo.getSecret()
		);
		
		httpResponse = updateRequest.toHTTPRequest().send();
		
		regResponse = OIDCClientRegistrationResponseParser.parse(httpResponse);
		
		if (! regResponse.indicatesSuccess()) {
			// We have an error
			ClientRegistrationErrorResponse errorResponse = (ClientRegistrationErrorResponse)regResponse;
			System.err.println(errorResponse.getErrorObject());
			return;
		}
		
		// Success
		successResponse = (OIDCClientInformationResponse)regResponse;
		
		// Ensure the client name has been updated
		clientInfo = successResponse.getOIDCClientInformation();
		System.out.println("Client name: " + clientInfo.getMetadata().getName());
		
		
		// Request deletion
		ClientDeleteRequest deleteRequest = new ClientDeleteRequest(
			clientInfo.getRegistrationURI(),
			clientInfo.getRegistrationAccessToken()
		);
		
		httpResponse = deleteRequest.toHTTPRequest().send();
		
		regResponse = ClientRegistrationResponse.parse(httpResponse);
		
		if (! regResponse.indicatesSuccess()) {
			// We have an error
			ClientRegistrationErrorResponse errorResponse = (ClientRegistrationErrorResponse)regResponse;
			System.err.println(errorResponse.getErrorObject());
		}
		
		// Success: nothing returned
	}
}