import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { DocumentTypeTeacher } from './components/teacher/DocumentTypeTeacher';
import { DocumentAngelTeacher } from './components/teacher/DocumentAngelTeacher';

export default class App extends Component {
  displayName = App.name

  render() {
    return (
      <Layout>
        <Route exact path='/document/type/teacher' component={DocumentTypeTeacher} />
        <Route path='/document/angel/teacher' component={DocumentAngelTeacher} />
      </Layout>
    );
  }
}
