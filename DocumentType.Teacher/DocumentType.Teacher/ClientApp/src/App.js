import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { DocumentTypeTeacher } from './components/teacher/DocumentTypeTeacher';

export default class App extends Component {
  displayName = App.name

  render() {
    return (
      <Layout>
        <Route exact path='/document/type/teacher' component={DocumentTypeTeacher} />
      </Layout>
    );
  }
}
