import React from 'react'
import ReactDOM from 'react-dom'
import './index.css'
import App from './App'
import * as serviceWorker from './serviceWorker'

// NEW IMPORTS ref: https://www.youtube.com/watch?v=iNStN6q_5As
import  { ApolloClient, InMemoryCache } from 'apollo-boost'
import { ApolloProvider } from '@apollo/react-hooks'

import { createHttpLink, HttpLink } from 'apollo-link-http'
import { BrowserRouter } from 'react-router-dom'
import { setContext } from 'apollo-link-context'
import { AUTH_TOKEN } from './constants'

// Subscriptions
import { split } from 'apollo-link'
import { WebSocketLink } from 'apollo-link-ws'
import { getMainDefinition } from 'apollo-utilities'


const httpLink = new HttpLink({
  uri: 'https://ciao-be-2.herokuapp.com/'
})

const authLink = setContext((_, {headers}) => {
  const token = localStorage.getItem(AUTH_TOKEN)
  return{
    headers: {
      ...headers,
      authorization: token ? `Bearer ${token}` : ''
    }
  }
})

const wsLink = new WebSocketLink({
  uri: `wss://ciao-be-2.herokuapp.com/`,
  options: {
    reconnect: true,
    connectionParams: {
      token: localStorage.getItem("d65d2435915eab95eea7abfed9359381619dafa9e5bd44389285ebde40cb18fd10ba8b11133caf339ade087075fb2a06"),
    },
  }
})

const link = split(
  ({ query }) => {
    const { kind, operation } = getMainDefinition(query)
    return kind === 'OperationDefinition' && operation === 'subscription'
  },
  wsLink,
  authLink.concat(httpLink)
)

const cache = new InMemoryCache()

const client = new ApolloClient({
  link,
  cache,
  resolvers: {},
})

ReactDOM.render(
  <BrowserRouter>
    <ApolloProvider client={client}>
      <App />
    </ApolloProvider>
  </BrowserRouter>,
  document.getElementById('root')
)

serviceWorker.unregister()
