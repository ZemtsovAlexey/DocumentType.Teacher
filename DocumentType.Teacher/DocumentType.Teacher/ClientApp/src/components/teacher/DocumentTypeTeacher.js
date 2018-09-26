import React, {Component} from 'react';
import { HubConnectionBuilder, LogLevel } from '@aspnet/signalr';
import { Col, Row, Thumbnail, ButtonGroup, Button, FormGroup, FormControl, ControlLabel, Label } from 'react-bootstrap';
import { NetSettings } from './NetSettings';

export class DocumentTypeTeacher extends Component {
    displayName = DocumentTypeTeacher.name;

    constructor(props) {
        super(props);
        this.state = {
            hubConnection: null,
            file: null,
            fileUploading: false,
            iteration: 0,
            error: 0,
            successes: 0,
            successPercent: 0,
            computeResult: 0,
            imageSrc: '/thumbnail.png'
        };

        this.onFormSubmit = this.onFormSubmit.bind(this);
        this.onChange = this.onChange.bind(this);
        this.fileUpload = this.fileUpload.bind(this);
    }

    componentDidMount = () => {
        const hubConnection = new HubConnectionBuilder()
            .withUrl(`${document.location.protocol}//${document.location.host}/teacherHub`)
            .configureLogging(LogLevel.Information)
            .build();

        this.setState({hubConnection}, () => {
            this.state.hubConnection
                .start()
                .then(() => console.log('Connection started!'))
                .catch(err => console.log('Error while establishing connection :('));

            this.state.hubConnection.on('IterationChange', (result) => {
                this.setState({ iteration: result.iteration, error: result.error, successes: result.successes, successPercent: result.successPercent });
            });
        });
    };

    onFormSubmit = async (e) => {
        e.preventDefault();
        this.setState({ fileUploading: true });

        this.fileUpload(this.state.file)
            .then(response => response.blob())
            .then(images => {
                let outside = URL.createObjectURL(images);
                this.setState({ imageSrc: outside, fileUploading: false });
            });
    };
    
    onChange(e) {
        this.setState({file:e.target.files[0]})
    }

    fileUpload(file){
        let data = new FormData();
        data.append('file', file);
        
        return fetch('api/net/Compute/image', {
            method: 'POST',
            body: data
        });
    }

    async teachRun(){
        await fetch('api/net/teach/run', {
            method: 'POST'
        });
    }

    async teachStop(){
        await fetch('api/net/teach/stop', {
            method: 'POST'
        });
    }

    render() {
        return (
            <div style={{ margin: '0 0 25px 0' }}>
                <h1>Teacher</h1>

                <Row>
                    <Col sm={7}>
                        <Thumbnail href="#" alt="171x180" src={this.state.imageSrc} />
                    </Col>
                    <Col sm={4}>
                        <NetSettings />
                    </Col>
                </Row>
                <Row>
                    <Col sm={3}>
                        <form onSubmit={this.onFormSubmit} style={{display: "inline"}}>
                            <h1>File Upload</h1>
                            <input type="file" onChange={this.onChange} />
                            <Button type="submit" bsStyle="primary" disabled={this.state.fileUploading}>Upload</Button>
                            <label>{this.state.computeResult}</label>
                        </form>
                    </Col>
                    <Col sm={9}></Col>
                </Row>
                <FormGroup>
                    <ControlLabel htmlFor="fileUpload" style={{ cursor: "pointer" }}><h3><Label bsStyle="success">Add file</Label></h3>
                        <FormControl
                            id="fileUpload"
                            type="file"
                            accept=".jpg"
                            //onChange={this.addFile}
                            style={{ display: "none" }}
                        />
                    </ControlLabel>
                </FormGroup>
                <Row>
                    <Col sm={2}>
                        <ButtonGroup>
                            <Button onClick={this.teachRun}>teach run</Button>
                            <Button onClick={this.teachStop}>teach stop</Button>
                        </ButtonGroup>
                    </Col>
                    <Col sm={1}>
                    </Col>
                    <Col sm={1}>
                        <label>Iteration: </label>
                        <label>{this.state.iteration}</label>
                    </Col>
                    <Col sm={2}>
                        <label>Error: </label>
                        <label>{this.state.error}</label>
                    </Col>
                    <Col sm={3}>
                        <label>Successes: </label>
                        <label>{this.state.successes}</label>
                        <label>&nbsp;&nbsp;{this.state.successPercent}%</label>
                    </Col>
                </Row>
            </div>
        );
    }
}
